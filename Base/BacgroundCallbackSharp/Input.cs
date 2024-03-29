﻿using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;

using Linearstar.Windows.RawInput;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    ///<summary>
    ///<br><see langword="En"/></br>
    ///<br/>The class creates a proxy <see cref="HwndSource"/>. Registers it to receive mouse and keyboard events. Creates classes to handle events.
    ///<br><see langword="Ru"/></br>
    ///<br>Класс создает прокси-источник HwndSource. Регистрирует его для получения событий мыши и клавиатуры. Создает классы для обработки событий.</br>
    ///</summary>
    public partial class Input : IDisposable
    {
        /// <summary>
        /// Примечание: WS_POPUP в Windows API обычно представлен как 32-битное значение (Int32),
        /// но для ясности и соответствия документации используется тип long (Int64).
        /// При приведении значения к типу int происходит потеря старших битов, но это безопасно,
        /// так как младшие 32 бита достаточны для представления стиля окна WS_POPUP.
        /// </summary>
        private const long WS_POPUP = 0x80000000L;
        private const int WM_INPUT = 0x00FF;
        private bool isDispose = false;
        private bool _isInitialized = false;
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly IMouseHandler _mouseHandler;
        private readonly HandlersInput _initInput;
        private IKeyboardCallback? _callbackFunction;
        private readonly Action<RawInputKeyboardData> _callbackEventKeyboardData;
        private readonly Action<RawInputMouseData> _callbackEventMouseData;
        private volatile HwndSource? _proxyInputHandlerWindow;
        private LowLevelKeyHook? _lowLevelHook;
        private Thread? _winThread;

        public Input() : this(null, HandlersInput.Keyboard | HandlersInput.Mouse) { }
        public Input(IMouseHandler? mouseHandler = null, HandlersInput initInputHandle = HandlersInput.Keyboard | HandlersInput.Mouse)
        {
            _initInput = initInputHandle;

            _keyboardHandler = new KeyboardHandler();
            _mouseHandler = mouseHandler is IMouseHandler handlerMouse ? handlerMouse : new MouseHandler();

            _callbackEventKeyboardData = new Action<RawInputKeyboardData>((x) => _keyboardHandler.HandlerKeyboard(x));
            _callbackEventMouseData = new Action<RawInputMouseData>((x) => _mouseHandler.HandlerMouse(x));

            Task waitForInitialization = Task.Run(Initialization);
            waitForInitialization.Wait();
            this._initInput = initInputHandle;
        }
        public void Dispose()
        {
            if (isDispose is true) return;

            _proxyInputHandlerWindow?.Dispatcher?.InvokeShutdown();
            _lowLevelHook?.Dispose();
            isDispose = true;
            GC.SuppressFinalize(this);
        }
        ~Input()
        {
            if (isDispose is true) return;
            try
            {
                _lowLevelHook?.Dispose();
                _proxyInputHandlerWindow?.Dispose();
            }
            catch { }
        }
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IKeyboardHandler"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Ссылка на класс, реализующий интерфейс <see cref="IKeyboardHandler"/>.</br>
        ///</returns>      
        public IKeyboardHandler GetKeyboardHandler() => _keyboardHandler;
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IMouseHandler"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Ссылка на класс, реализующий интерфейс <see cref="IMouseHandler"/>.</br>
        ///</returns>
        public IMouseHandler GetMouseHandler() => _mouseHandler;
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IKeyboardCallback"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Ссылка на класс, реализующий интерфейс <see cref="IKeyboardCallback"/>.</br>
        ///</returns>
        public IKeyboardCallback GetKeyboardCallbackFunction() => _callbackFunction is IKeyboardCallback CallBack ? CallBack : throw new NullReferenceException(nameof(_callbackFunction));
        private Task Initialization()
        {
            if (_isInitialized is true) throw new InvalidOperationException($"The object({nameof(Input)}) cannot be re-initialized");

            Task InitThreadAndSetWindowsHandler = Task.Run(() =>
            {
                _winThread = new Thread(() =>
                {
                    HwndSourceParameters configInitWindow = new HwndSourceParameters($"InputHandler-{Path.GetRandomFileName}", 0, 0)
                    {
                        WindowStyle = unchecked((int)WS_POPUP)
                    };
                    _proxyInputHandlerWindow = new HwndSource(configInitWindow);

                    Dispatcher.Run();
                })
                { Name = "Input Handler" };
                _winThread.SetApartmentState(ApartmentState.STA);
                _winThread.Start();
            });

            Task waitForDispatcherValidation = Task.Run(async () =>
            {
                Dispatcher? winDispatcher = Dispatcher.FromThread(_winThread);

                if (SpinWait.SpinUntil(() =>
                {
                    winDispatcher = Dispatcher.FromThread(_winThread);
                    return winDispatcher is not null;
                }, TimeSpan.FromMilliseconds(500)) is false) throw new NullReferenceException($"Failed to get window Dispatcher - {nameof(winDispatcher)} is null");

                /// <summary>
                /// Ранее была проблема, которое выражалось в том, что полученный объект Dispatcher был в не валидном состоянии.
                /// это проявляясь в том, что вызов Invoke() был бесконечным (вроде), а await InvokeAsync() возвращал 
                /// ошибку TaskCanceledException, по которой я решил определять валидное состояние. Как только объект 
                /// принимал валидное состояние функция завершалось успешно. Мне не известны иные способы ожидания валидности данного объекта.
                /// И хотя проблема, сейчас не наблюдается, пусть проверка останется.
                /// </summary>
                bool isTimeoutInitializationDispatcher = false;
                System.Threading.Timer timeoutTimer = new System.Threading.Timer((_) => isTimeoutInitializationDispatcher = true);
                timeoutTimer.Change(TimeSpan.FromSeconds(4), Timeout.InfiniteTimeSpan);
                while (true)
                {
                    try
                    {
                        if (isTimeoutInitializationDispatcher is true) throw new TimeoutException(nameof(waitForDispatcherValidation));
                        Task taskWinInit = await winDispatcher.InvokeAsync(async () => await Task.Delay(1)).Task;
                        timeoutTimer.Dispose();
                        break;
                    }
                    catch (System.Threading.Tasks.TaskCanceledException) { }
                }
            });

            Task subscribeWindowToRawInput = new Task(async () =>
            {
                if (_proxyInputHandlerWindow is null) throw new NullReferenceException("The window could not initialize");

                IntPtr HandleWindow = _proxyInputHandlerWindow.Handle;
                List<(HidUsageAndPage InputType, RawInputDeviceFlags Mode, nint hWndTarget)> queryTypeList = [];
                await _proxyInputHandlerWindow.Dispatcher.InvokeAsync(() =>
                {
                    switch (_initInput)
                    {
                        case HandlersInput.Keyboard | HandlersInput.Mouse:
                            queryTypeList.Add((HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, HandleWindow));
                            queryTypeList.Add((HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        case HandlersInput.Keyboard:
                            queryTypeList.Add((HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        case HandlersInput.Mouse:
                            queryTypeList.Add((HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        default: throw new NullReferenceException(nameof(_initInput));
                    }
                    Array.ForEach(queryTypeList.ToArray(), (clientInput) => RawInputDevice.RegisterDevice(clientInput.InputType, clientInput.Mode, clientInput.hWndTarget));

                    _proxyInputHandlerWindow.AddHook(WndProc);

                    nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
                    {
                        if (msg is WM_INPUT)
                        {
                            if (RawInputData.FromHandle(lParam) is RawInputData data)
                            {
                                switch (data)
                                {
                                    case RawInputKeyboardData keyboardData:
                                        _callbackEventKeyboardData.Invoke(keyboardData);
                                        break;

                                    case RawInputMouseData mouseData:
                                        _callbackEventMouseData.Invoke(mouseData);
                                        break;
                                }
                            }
                        }
                        return hwnd;
                    }

                    _lowLevelHook = new LowLevelKeyHook();
                    _lowLevelHook.InstallHook();

                    CallbackFunctionKeyboard callbackFunctionKeyboard = new CallbackFunctionKeyboard(_keyboardHandler, _lowLevelHook);
                    _callbackFunction = callbackFunctionKeyboard;

                }, DispatcherPriority.Render);
            });
            Task.WaitAll(InitThreadAndSetWindowsHandler, waitForDispatcherValidation);
            subscribeWindowToRawInput.Start();
            subscribeWindowToRawInput.Wait();
            _isInitialized = true;
            return Task.CompletedTask;
        }
    }
}