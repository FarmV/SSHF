using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Views.Windows.Notify;
using System.Threading;
using Linearstar.Windows.RawInput;
using System.Windows.Interop;
using System.Collections.Concurrent;
using System.Windows.Threading;
using SSHF.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FVH.Background.Input;
using System.Linq.Expressions;

namespace SSHF
{
    internal static class Program
    {
        internal static bool DesignerMode = true;
        internal static event EventHandler? DpiChange;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private static CancellationTokenSource _tokenShutdownHost = new CancellationTokenSource();
        private static IHost _host = _host ?? throw new NullReferenceException("Host is not initialized");
        private static IServiceProvider _serviceProvider = _serviceProvider ?? throw new NullReferenceException($"ServiceProvider is not initialized");
        private static Mutex? _mutexSingleInstance;
        private static Dependencie GetDependencie<Dependencie>() where Dependencie : notnull => _serviceProvider.GetRequiredService<Dependencie>();
        [STAThread]
        private static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "InitializedMain";
            System.Windows.Application app = new System.Windows.Application();
            app.Startup += async (_, _) => await AppStartupAsync(args);
            app.Run();
        }
        private static async Task AppStartupAsync(string[] args)
        {
            DesignerMode = false;

            bool singleInstance;
            try
            {
                _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out singleInstance);
            }
            catch
            {
                Application.Current.Shutdown();
                return;
            }
            if(singleInstance is false) Application.Current.Shutdown();

            _host = BasicDependencies.ConfigureDependencies(args);
            _serviceProvider = _host.Services;

            SubscribeDpiChange();

            await RegisterKeysForMainWinodw();

            await _host.RunAsync(_tokenShutdownHost.Token);
        }
        private static async Task RegisterKeysForMainWinodw()
        {         
            Input input = GetDependencie<Input>();
            MainWindow mainWindow = GetDependencie<MainWindow>();
            IKeyboardCallback keyboardCallback = input.GetKeyboardCallbackFunction();
            //Показ окна с изображеие
            await keyboardCallback.AddCallbackTask(new VKeys[]
            {
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            },
            () => new Task(() => mainWindow.Dispatcher.Invoke(() => ((System.Windows.Input.ICommand)((MainWindowViewModel)mainWindow.DataContext).InvoceRefreshWindow).Execute(default))),
            nameof(MainWindowViewModel.InvoceRefreshWindow));
            //Юлокировка перемещение окна
            await keyboardCallback.AddCallbackTask(new VKeys[]
            {
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            },
            () => new Task(() =>
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindowViewModel viewModel = (MainWindowViewModel)mainWindow.DataContext;
                    if (viewModel.IsRefreshWindow is true) return;
                    viewModel.BlockRefresh = !viewModel.BlockRefresh;
                });
            }), nameof(MainWindowViewModel.BlockRefresh));
        }
        internal static void Shutdown()
        {
            _tokenShutdownHost?.Cancel();
            if (_host.Services.GetService<Input>() is Input input) input.Dispose();
            _mutexSingleInstance?.ReleaseMutex();
            _mutexSingleInstance?.Close();
            System.Windows.Application.Current.Shutdown();
        }
        private static void SubscribeDpiChange()
        {
            MainWindow mainWindow = GetDependencie<MainWindow>();
            if (PresentationSource.FromVisual(mainWindow) is not HwndSource win32MainWindow) throw new InvalidOperationException();
            win32MainWindow.AddHook(WndProc);

            static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                const int WM_DPICHANGED = 0x02E0;

                switch (msg)
                {
                    case WM_DPICHANGED:
                        {
                            _ = Task.Run(() => { DpiChange?.Invoke(default, EventArgs.Empty); }).ConfigureAwait(false);
                            //handled = true; ??
                        }
                        break;
                }
                return hwnd;
            }
        }
        private async static Task AssociateWindowClosingWithTheHost()
        {
            MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            await mainWindow.Dispatcher.InvokeAsync(() => mainWindow.Closed += (_, _) => _tokenShutdownHost.Cancel());
        }

      
       
        private static partial class BasicDependencies
        {
            internal static IHost ConfigureDependencies(string[]? args = null) => Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
            { configuration.Sources.Clear(); }).ConfigureServices((_, container) =>
            {
                container.AddSingleton<Input>(CreateHadnlerInput());
                container.AddSingleton<MainWindow>(CreateMainWindow());
                container.AddSingleton<Notificator>(CreateAnIconInTheNotificationArea(container.BuildServiceProvider().GetRequiredService<Input>().GetMouseHandler()));
            }).Build();
            private static MainWindow CreateMainWindow(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                MainWindow? mainWindow = null;
                uiDispatcher.Invoke(() =>
                {
                    mainWindow = new MainWindow()
                    {
                        DataContext = new MainWindowViewModel()
                    };
                });
                if (mainWindow is null) throw new NullReferenceException();
                mainWindow.Show(); //todo Проверить нужно ли показывать окно
                mainWindow.Hide();
                return mainWindow;
            }
            private static Notificator CreateAnIconInTheNotificationArea(IMouseHandler input)
            {
                Notificator notificationTrayIconWindow = new Notificator();
                notificationTrayIconWindow.DataContext = new NotificatorViewModel(notificationTrayIconWindow, input);
                notificationTrayIconWindow.Show(); //todo Проверить нужно ли показывать окно
                notificationTrayIconWindow.Hide();
                return notificationTrayIconWindow;
            }
            private static Input CreateHadnlerInput(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                Input? inputHendler = null;
                uiDispatcher.Invoke(() => inputHendler = new Input());
                return inputHendler ?? throw new NullReferenceException();
            }
        }
    }
}


//    public partial class App : System.Windows.Application, IAsyncDisposable
//    {
//        protected override async void OnExit(ExitEventArgs e) => await DisposeAsync();

//        public async ValueTask DisposeAsync()
//        {
//            if (CallbackFunction is not null) await CallbackFunction.DisposeAsync();
//            GC.SuppressFinalize(this);
//        }

//        internal static KeyboardKeyCallbackFunction? CallbackFunction;

//        internal static bool IsDesignMode { get; private set; } = true;

//        internal static event EventHandler<SSHF.Infrastructure.Algorithms.Input.RawInputEvent>? Input;

//        internal static event EventHandler? DPIChange;

//        internal const string GetMyMainWindow = "Главное окно приложения - поток";
//        internal const string GetWindowNotification = "Нотификация окно - поток";
//        internal volatile static ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>> WindowsIsOpen = new ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>>();

//        private static NotificatorViewModel? Notificator;
//        internal static NotificatorViewModel GetNotificator() => Notificator is null ? throw new NullReferenceException("Нотификатор не инициализирован") : Notificator;

//        protected override void OnStartup(StartupEventArgs e)
//        {
//            Thread.CurrentThread.Name = "General stream!";
//            IsDesignMode = false;
//            // SystemParameters
//            if (IsDesignMode is false)
//            {

//                _ = StartConfigurations();

//            }
//            base.OnStartup(e);
//        }

//        static private Task StartConfigurations() => _ = Task.Run(() =>
//        {
//            Thread[] startInit = new Thread[]
//            {
//                 new Thread(() =>
//                 {
//                      Dispatcher dispThreadMainWindow = Dispatcher.CurrentDispatcher;
//                      Thread.CurrentThread.Name = GetMyMainWindow;
//                      MainWindow mainWindow = new MainWindow
//                      {
//                          Tag = GetMyMainWindow,
//                          DataContext = new MainWindowViewModel()
//                      };
//                      mainWindow.Show();
//                      mainWindow.Hide();
//                      if(WindowsIsOpen.TryAdd(GetMyMainWindow, new KeyValuePair<Window,Dispatcher>(mainWindow, dispThreadMainWindow)) is not true) throw new InvalidOperationException();
//                      System.Windows.Threading.Dispatcher.Run();
//                 }),
//                 new Thread(async () =>
//                 {
//                     Dispatcher dispThreadNotification = Dispatcher.CurrentDispatcher;

//                     Thread.CurrentThread.Name = GetWindowNotification;
//                     Notificator myNotification = new Notificator
//                     {
//                         Tag = GetWindowNotification,
//                     };
//                     NotificatorViewModel noti = new NotificatorViewModel();
//                     Notificator = noti;
//                     myNotification.DataContext = noti;

//                     myNotification.Show();
//                     myNotification.Hide();

//                     await using TrayIcon icon = new TrayIcon();

//                     if(WindowsIsOpen.TryAdd(GetWindowNotification, new KeyValuePair<Window,Dispatcher>(myNotification, dispThreadNotification)) is not true) throw new InvalidOperationException();

//                     System.Windows.Threading.Dispatcher.Run();
//                 })
//            };

//            _ = Task.Run(() =>
//            {
//                startInit.AsParallel().ForAll(x =>
//                {
//                    x.SetApartmentState(ApartmentState.STA);
//                    x.Start();
//                });

//                while (WindowsIsOpen.Count is not 2) { }
//            }).ContinueWith(async t =>
//            {
//                Dispatcher dispThreadMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;
//                await dispThreadMainWindow.InvokeAsync(() =>
//                {
//                    Thread dispMain = dispThreadMainWindow.Thread;
//                    dispMain.Name = GetMyMainWindow;
//                    Dispatcher disMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;

//                    Window mainWindow = WindowsIsOpen[GetMyMainWindow].Key;

//                    if (WindowsIsOpen.TryUpdate(GetMyMainWindow, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetMyMainWindow]) is not true) throw new InvalidOperationException();
//                }, DispatcherPriority.Render);

//                _ = WindowsIsOpen[GetMyMainWindow].Value.InvokeAsync(() =>
//                {
//                    if (PresentationSource.FromVisual(WindowsIsOpen[GetMyMainWindow].Key) is not HwndSource source) throw new NullReferenceException("Не удалось получить HwndSource окна");

//                    RawInputDeviceRegistration[] devices =
//                    {
//                             new RawInputDeviceRegistration(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle),
//                             new RawInputDeviceRegistration(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, source.Handle)
//                    };

//                    RawInputDevice.RegisterDevice(devices);
//                    source.AddHook(WndProc);

//                    static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
//                    {
//                        const int WM_INPUT = 0x00FF;
//                        const int WM_DPICHANGED = 0x02E0;

//                        switch (msg)
//                        {
//                            case WM_INPUT:
//                                {


//                                    RawInputData? data = RawInputData.FromHandle(lParam); // нельзя асинхронно
//                                    Input?.Invoke(WindowsIsOpen[GetMyMainWindow].Key, new Infrastructure.Algorithms.Input.RawInputEvent(data));
//                                }
//                                break;
//                            case WM_DPICHANGED:
//                                {
//                                    _ = Task.Run(() => { DPIChange?.Invoke(new object(), EventArgs.Empty); }).ConfigureAwait(false);
//                                }
//                                break;
//                        }
//                        return hwnd;
//                    }
//                }, DispatcherPriority.Render);

//            }).ContinueWith((taskInTask) =>
//            {
//                _ = Task.Run(() => { CallbackFunction = KeyboardKeyCallbackFunction.GetInstance(); });


//                Dispatcher dispThreadMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

//                _ = dispThreadMainWindow.InvokeAsync(() =>
//                {
//                    Thread dispMain = dispThreadMainWindow.Thread;
//                    dispMain.Name = GetWindowNotification;
//                    Dispatcher disMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

//                    Window mainWindow = WindowsIsOpen[GetWindowNotification].Key;

//                    if (WindowsIsOpen.TryUpdate(GetWindowNotification, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetWindowNotification]) is not true) throw new InvalidOperationException();
//                }, DispatcherPriority.Render);
//            }).ContinueWith((T) =>
//            {
//                // Регстрация функций
//                //Depl translete

//                KeyboardKeyCallbackFunction callback = KeyboardKeyCallbackFunction.GetInstance();
//                string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\app-3.4.15088\DeepL.exe";
//                string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";
//                //var keyCombianteionGetTranslate = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
//                //{
//                //Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_1,
//                //Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_2,
//                //Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_3
//                //};
//                //callback.AddCallBackTask(keyCombianteionGetTranslate, () => new Task(async () =>
//                //{
//                //    AlgorithmGetTranslateAbToDepl instance = await AlgorithmGetTranslateAbToDepl.GetInstance(DeeplDirectory, ScreenshotReaderDirectory);
//                //    try
//                //    {
//                //        await instance.Start<string, bool>(false);
//                //    } catch (Exception) { }
//                //})).ConfigureAwait(false);


//                // показ окна Fast
//                var keyCombianteionGetClipboardImageAndRefresh = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
//                {
//                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_LWIN,
//                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_SHIFT,
//                 Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_A
//                };
//                callback.AddCallBackTask(keyCombianteionGetClipboardImageAndRefresh, () => new Task(() =>
//                {
//                    _ = WindowsIsOpen[GetMyMainWindow].Value.InvokeAsync(() => ((System.Windows.Input.ICommand)(((MainWindowViewModel)WindowsIsOpen[GetMyMainWindow].Key.DataContext).InvoceRefreshWindow)).Execute(new object()));

//                })).ConfigureAwait(false);
//                // блок обновления окна
//                var keyCombianteionBlockWindowRefreshFast = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
//                {
//                  Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL,
//                  Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CAPITAL,

//                };
//                callback.AddCallBackTask(keyCombianteionBlockWindowRefreshFast, () => new Task(async () =>
//                {
//                    await App.WindowsIsOpen[App.GetMyMainWindow].Value.InvokeAsync(() =>
//                    {
//                        MainWindowViewModel ViewModel = (MainWindowViewModel)WindowsIsOpen[GetMyMainWindow].Key.DataContext;
//                        if (ViewModel.IsRefreshWindow is true) return;
//                        ViewModel.BlockRefresh = !ViewModel.BlockRefresh;
//                    });

//                })).ConfigureAwait(false);


//            });
//        });

//    }
//}
