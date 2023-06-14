using FVH.Background.Input;


using Linearstar.Windows.RawInput;

using System;
using System.IO;
using System.Security.Policy;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Linq;

using static FVH.Background.Input.KeyboardHandler;

namespace FVH.Background.Input
{
    /// <summary>
    /// <br><see langword="En"/></br>
    ///<br/>The class creates a proxy <see cref="HwndSource"/>. Registers it to receive mouse and keyboard events. Creates classes to handle events.
    ///<br><see langword="Ru"/></br>
    ///<br>Класс создает прокси-источник HwndSource. Регистрирует его для получения событий мыши и клавиатуры. Создает классы для обработки событий.</br>
    ///</summary>
    public class Input : IDisposable
    {
        private volatile HwndSource? ProxyInputHandlerWindow;

        private bool isDispose = false;
        public void Dispose()
        {
            if (isDispose is true) return;

            ProxyInputHandlerWindow?.Dispatcher?.InvokeShutdown();
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
                ProxyInputHandlerWindow?.Dispose();
            }
            catch { }
        }


        private bool isItialized = false;
        private readonly Action<RawInputKeyboardData> _callbackEventKeyboardData;
        private readonly Action<RawInputMouseData> _callbackEventMouseData;
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly IMouseHandler _mouseHandler;
        private LowLevlHook? _lowLevlHook;
        private IKeyboardCallback? _callbackFunction;
        private Thread? winThread;
        private readonly HandlersInput initInput;


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

        [Flags]
        public enum HandlersInput
        {
            Keyboard = 1,
            Mouse = 2,
        }


        public Input() : this(null, HandlersInput.Keyboard | HandlersInput.Mouse) { }
        public Input(IMouseHandler? mouseHandler = null, HandlersInput initInputHandle = HandlersInput.Keyboard | HandlersInput.Mouse)
        {
            initInput = initInputHandle;

            _keyboardHandler = new KeyboardHandler();
            _mouseHandler = mouseHandler is IMouseHandler handlerMouse ? handlerMouse : new MouseHandler();

            _callbackEventKeyboardData = new Action<RawInputKeyboardData>((x) => _keyboardHandler.HandlerKeyboard(x));
            _callbackEventMouseData = new Action<RawInputMouseData>((x) => _mouseHandler.HandlerMouse(x));

            Task waitForInitialization = Task.Run(async () => await Initialization());
            waitForInitialization.Wait();
            this.initInput = initInputHandle;
        }


        private TimeSpan TimeoutInitialization => TimeSpan.FromSeconds(10);
        private Task Initialization()
        {
            if (isItialized is true) throw new InvalidOperationException($"The object({nameof(Input)}) cannot be re-initialized");

            Task InitThreadAndSetWindowsHanlder = Task.Run(() =>
            {
                winThread = new Thread(() =>
                {
                    HwndSourceParameters configInitWindow = new HwndSourceParameters($"InputHandler-{Path.GetRandomFileName}", 0, 0)
                    {
                        WindowStyle = 0x800000
                    };
                    ProxyInputHandlerWindow = new HwndSource(configInitWindow);

                    Dispatcher.Run();
                })
                { Name = "Inupt Hanndler" };
                winThread.SetApartmentState(ApartmentState.STA);
                winThread.Start();
            });

            Task waitforWidnowDispather = Task.Run(async () =>
            {
                Dispatcher? winDispatcher = Dispatcher.FromThread(winThread);

                while (winDispatcher is null)
                {
                    winDispatcher = Dispatcher.FromThread(winThread);
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
                if (ProxyInputHandlerWindow is null) throw new NullReferenceException("The window could not initialize");

                IntPtr HandleWindow = ProxyInputHandlerWindow.Handle;
                List<(HidUsageAndPage, RawInputDeviceFlags, IntPtr)> devices = new();
                RawInputDeviceRegistration[] devices1 = new RawInputDeviceRegistration[2];
                ProxyInputHandlerWindow.Dispatcher.Invoke(() => // синхронно?
                {
                    switch (initInput)
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
                        default: throw new NullReferenceException(nameof(initInput));
                    }
                    Array.ForEach(devices.ToArray(), (x) => RawInputDevice.RegisterDevice(x.Item1, x.Item2, x.Item3));


                    ProxyInputHandlerWindow.AddHook(WndProc);

                    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
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
            isItialized = true;
            return Task.CompletedTask;
        }
    }
}



