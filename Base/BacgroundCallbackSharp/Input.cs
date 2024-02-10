using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;

using Linearstar.Windows.RawInput;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    /// <summary>
    /// <br><see langword="En"/></br>
    ///<br/>The class creates a proxy <see cref="HwndSource"/>. Registers it to receive mouse and keyboard events. Creates classes to handle events.
    ///<br><see langword="Ru"/></br>
    ///<br>Класс создает прокси-источник HwndSource. Регистрирует его для получения событий мыши и клавиатуры. Создает классы для обработки событий.</br>
    ///</summary>
    public partial class Input : IDisposable
    {
        /// <summary>
        /// Примечание: WS_POPUP в Windows API обычно представлен как 32-битное значение (Int32),
        /// но для ясности и соответствия документации используетcя тип long (Int64).
        /// При приведении значения к типу int происходит потеря старших битов, но это безопасно,
        /// так как младшие 32 бита достаточны для представления стиля окна WS_POPUP.
        /// </summary>
        private const long WS_POPUP = 0x80000000L;
        private bool isDispose = false;
        private bool _isInitialized = false;
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly IMouseHandler _mouseHandler;
        private readonly HandlersInput _initInput;
        private IKeyboardCallback? _callbackFunction;
        private readonly Action<RawInputKeyboardData> _callbackEventKeyboardData;
        private readonly Action<RawInputMouseData> _callbackEventMouseData;
        private volatile HwndSource? _proxyInputHandlerWindow;
        private LowLevlHook? _lowLevlHook;
        private Thread? _winThread;

        public Input() : this(null, HandlersInput.Keyboard | HandlersInput.Mouse) { }
        public Input(IMouseHandler? mouseHandler = null, HandlersInput initInputHandle = HandlersInput.Keyboard | HandlersInput.Mouse)
        {
            _initInput = initInputHandle;

            _keyboardHandler = new KeyboardHandler();
            _mouseHandler = mouseHandler is IMouseHandler handlerMouse ? handlerMouse : new MouseHandler();

            _callbackEventKeyboardData = new Action<RawInputKeyboardData>((x) => _keyboardHandler.HandlerKeyboard(x));
            _callbackEventMouseData = new Action<RawInputMouseData>((x) => _mouseHandler.HandlerMouse(x));

            Task waitForInitialization = Task.Run(async () => await Initialization());
            waitForInitialization.Wait();
            this._initInput = initInputHandle;
        }
        public void Dispose()
        {
            if (isDispose is true) return;

            _proxyInputHandlerWindow?.Dispatcher?.InvokeShutdown();
            _lowLevlHook?.Dispose();
            isDispose = true;
            GC.SuppressFinalize(this);
        }
        ~Input()
        {
            if (isDispose is true) return;
            try
            {
                _lowLevlHook?.Dispose();
                _proxyInputHandlerWindow?.Dispose();
            }
            catch { }
        }
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IKeyboardHandler"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Cсылка на класс, реализующий интерфейс <see cref="IKeyboardHandler"/>.</br>
        ///</returns>      
        public IKeyboardHandler GetKeyboardHandler() => _keyboardHandler;
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IMouseHandler"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Cсылка на класс, реализующий интерфейс <see cref="IMouseHandler"/>.</br>
        ///</returns>
        public IMouseHandler GetMouseHandler() => _mouseHandler;
        ///<returns>
        /// <br><see langword="En"/></br>
        /// <br>Reference to the class that implements the <see cref="IKeyboardCallback"/>.</br>
        /// <br><see langword="Ru"/></br>
        /// <br>Cсылка на класс, реализующий интерфейс <see cref="IKeyboardCallback"/>.</br>
        ///</returns>
        public IKeyboardCallback GetKeyboardCallbackFunction() => _callbackFunction is IKeyboardCallback CallBack ? CallBack : throw new NullReferenceException(nameof(_callbackFunction));
        private static TimeSpan TimeoutInitialization => TimeSpan.FromSeconds(10);
        private Task Initialization()
        {
            if (_isInitialized is true) throw new InvalidOperationException($"The object({nameof(Input)}) cannot be re-initialized");

            Task InitThreadAndSetWindowsHanlder = Task.Run(() =>
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
                { Name = "Inupt Handler" };
                _winThread.SetApartmentState(ApartmentState.STA);
                _winThread.Start();
            });

            Task waitforWidnowDispather = Task.Run(async () =>
            {
                Dispatcher? winDispatcher = Dispatcher.FromThread(_winThread);

                while (winDispatcher is null)
                {
                    winDispatcher = Dispatcher.FromThread(_winThread);
                }

                bool TimeoutInitDispathcer = false;
                System.Threading.Timer Timer = new System.Threading.Timer((_) => TimeoutInitDispathcer = true);
                Timer.Change(TimeoutInitialization, Timeout.InfiniteTimeSpan);
                while (true)
                {
                    try
                    {
                        if (TimeoutInitDispathcer is true) throw new InvalidOperationException(nameof(TimeoutInitDispathcer));
                        Task taskWinInit = await winDispatcher.InvokeAsync(async () => await Task.Delay(1)).Task;
                        Timer.Dispose();
                        break;
                    }
                    catch (System.Threading.Tasks.TaskCanceledException) { }
                }
            });

            Task subscribeWindowtoRawInput = new Task(() =>
            {
                if (_proxyInputHandlerWindow is null) throw new NullReferenceException("The window could not initialize");

                IntPtr HandleWindow = _proxyInputHandlerWindow.Handle;
                List<(HidUsageAndPage, RawInputDeviceFlags, IntPtr)> devices = new();
                RawInputDeviceRegistration[] devices1 = new RawInputDeviceRegistration[2];
                _proxyInputHandlerWindow.Dispatcher.Invoke(() => // синхронно?
                {
                    switch (_initInput)
                    {
                        case HandlersInput.Keyboard | HandlersInput.Mouse:
                            devices.Add((HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, HandleWindow));
                            devices.Add((HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        case HandlersInput.Keyboard:
                            devices.Add((HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        case HandlersInput.Mouse:
                            devices.Add((HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, HandleWindow));
                            break;
                        default: throw new NullReferenceException(nameof(_initInput));
                    }
                    Array.ForEach(devices.ToArray(), (x) => RawInputDevice.RegisterDevice(x.Item1, x.Item2, x.Item3));

                    _proxyInputHandlerWindow.AddHook(WndProc);

                    nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
                    {
                        const int WM_INPUT = 0x00FF;
                        switch (msg)
                        {
                            case WM_INPUT:
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
                                break;
                        }
                        return hwnd;
                    }

                    _lowLevlHook = new LowLevlHook(); // он должен быть создан потоком владецем окна?
                    _lowLevlHook.InstallHook();
                    _callbackFunction = new CallbackFunctionKeyboard(_keyboardHandler, _lowLevlHook);
                }, DispatcherPriority.Render);

            });
            Task.WaitAll(InitThreadAndSetWindowsHanlder, waitforWidnowDispather);
            subscribeWindowtoRawInput.Start();
            subscribeWindowtoRawInput.Wait();
            _isInitialized = true;
            return Task.CompletedTask;
        }
    }
}



