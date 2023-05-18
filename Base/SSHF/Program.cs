using FVH.Background.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReactiveUI;

using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;



using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SSHF
{

    internal class App
    {
        internal static bool DesignerMode = true;
        internal static event EventHandler? DpiChange;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private CancellationTokenSource _tokenShutdownHost;
        private readonly IHost _host;
        private IServiceProvider _serviceProvider;
        private static Mutex? _mutexSingleInstance;

        private Dependencie GetDependencie<Dependencie>() where Dependencie : notnull => _serviceProvider.GetRequiredService<Dependencie>();
        private App(IHost host)
        {
            _tokenShutdownHost = new CancellationTokenSource();
            _host = host;
            _serviceProvider = _host.Services;
        }
        [STAThread]
        private static void Main(string[] args)
        {
            bool mutexWasCreated = false;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { Environment.Exit(-100501); }
            if (mutexWasCreated is false) Environment.Exit(-100501);
            Thread.CurrentThread.Name = "FVH Main Thread";
            System.Windows.Application application = new System.Windows.Application();
            application.Startup += async (_, _) => await Start(args).ConfigureAwait(false);
            application.Run();
        }
        internal static async Task Start(string[] args)
        {
            DesignerMode = false;
            App app = new App(BasicDependencies.ConfigureDependencies(Thread.CurrentThread));
            app._serviceProvider = app._host.Services;
            app.RegShortcuts();
            System.Windows.Application.Current.Exit += (_, _) => app.Shutdown();
            await app._host.StartAsync();
        }
        private void RegShortcuts() => GetDependencie<ShortcutsManager>().RegisterShortcuts();
        private async Task RegisterKeysForMainWinodw()
        {
            Input input = GetDependencie<Input>();
            MainWindow mainWindow = GetDependencie<MainWindow>();
            IKeyboardCallback keyboardCallback = input.GetKeyboardCallbackFunction();
            //Показ окна с изображеие
            await keyboardCallback.AddCallBackTask(new VKeys[]
            {
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            },
            () => new Task(() => mainWindow.Dispatcher.Invoke(() =>
            {
                MainWindowViewModel viewModel = (MainWindowViewModel)mainWindow.DataContext;
                viewModel.RefreshWindowInvoke.Execute();
            })));
            //Блокировка перемещение окна
            await keyboardCallback.AddCallBackTask(new VKeys[]
            {
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            },
            () => new Task(() =>
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindowViewModel viewModel = (MainWindowViewModel)mainWindow.DataContext;
                    if (viewModel.WindowPositionUpdater.IsUpdateWindow is true) return;
                    viewModel.SwithBlockRefreshWindow.Execute();
                });
            }), nameof(MainWindowViewModel.BlockRefresh));
        }
        internal async void Shutdown()
        {
            _tokenShutdownHost?.Cancel();
            if (_host.Services.GetService<Input>() is Input input) input.Dispose();
            _mutexSingleInstance?.ReleaseMutex();
            _mutexSingleInstance?.Close();
            if (_host.Services.GetService<TrayIconManager>() is TrayIconManager notificator) await notificator.DisposeAsync();
        }
        private void SubscribeDpiChange()
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
                            handled = true;
                        }
                        break;
                }
                return hwnd;
            }
        }
        private async Task AssociateWindowClosingWithTheHost()
        {
            MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            await mainWindow.Dispatcher.InvokeAsync(() => mainWindow.Closed += (_, _) => _tokenShutdownHost.Cancel());
        }
        private static class BasicDependencies
        {
            internal static IHost ConfigureDependencies(Thread uiThread, string[]? args = null) => Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((_, configuration) =>
            { configuration.Sources.Clear(); }).ConfigureServices((_, container) =>
            {
                Dispatcher uiDispatcher = GetWPFUIDispatcher(uiThread);

                uiDispatcher.Invoke(() =>
                {
                    RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;

                    container.AddSingleton<Dispatcher>(uiDispatcher);

                    container.AddSingleton<Input>(CreateHadnlerInput(uiDispatcher));

                    container.AddSingleton<IGetImage>(CreateImageProvider());

                    container.AddSingleton<MainWindow>(CreateMainWindow(uiDispatcher));
                    container.AddSingleton<MainWindowViewModel>(
                        CreateMainWindowViewModel(
                        CreateImageProvider(),
                        CreatePositionUpdaterWin32WPF(
                            container.BuildServiceProvider().GetRequiredService<MainWindow>()),
                        new DpiCorrector(container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<Dispatcher>()),
                        new SetImage(container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<Dispatcher>())));

                    SetDataContextMainWindow(
                        container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>()
                        );
                 
                    container.AddSingleton<Notificator>(
                        CreateAnIconInTheNotificationArea(container.BuildServiceProvider().GetRequiredService<Input>().GetMouseHandler(), ref container) //todo исправить затычку
                        );

                    container.AddSingleton<MainWindowCommand>(
                        CreateMainWindowCommand(
                          container.BuildServiceProvider().GetRequiredService<MainWindow>(),
                        container.BuildServiceProvider().GetRequiredService<MainWindowViewModel>(), 
                        container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardHandler())
                        );

                    container.AddSingleton<ShortcutsManager>(
                              CreateShortcutsManager(
                                                     container.BuildServiceProvider().GetRequiredService<Input>().GetKeyboardCallbackFunction(),
                                                               new IInvokeShortcuts[]
                                                               {
                                                                   container.BuildServiceProvider().GetRequiredService<MainWindowCommand>()
                                                               }));
                });
            }).Build();
            private static Dispatcher GetWPFUIDispatcher(Thread uiThread)  => Dispatcher.FromThread(uiThread) is not Dispatcher uiDispatcher ? throw new InvalidOperationException() : uiDispatcher;
            private static IWindowPositionUpdater CreatePositionUpdaterWin32WPF(Window window) => new Win32WPFWindowPositionUpdater(window);
            private static IGetImage CreateImageProvider() => new ImageManager();
            private static MainWindowViewModel CreateMainWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector corrector, SetImage setImage) =>
                           new MainWindowViewModel(imageProvider, windowPositionUpdater, corrector, setImage);

            private static void SetDataContextMainWindow(Window window, MainWindowViewModel mainWindowViewModel)
            {
                window.DataContext = mainWindowViewModel;
                ((IViewFor)window).ViewModel = mainWindowViewModel;
            }
            private static MainWindow CreateMainWindow(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                MainWindow? mainWindow = null;
                uiDispatcher.Invoke(() =>
                {
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                });
                if (mainWindow is null) throw new NullReferenceException();

                return mainWindow;
            }
            private static Notificator CreateAnIconInTheNotificationArea(IMouseHandler mouseHandler, ref IServiceCollection collection)
            {
                Notificator notificationTrayIconWindow = new Notificator();
                TrayIconManager trayIconManager = new TrayIconManager(notificationTrayIconWindow);
                collection.AddSingleton(trayIconManager);
                notificationTrayIconWindow.DataContext = new NotificatorViewModel(notificationTrayIconWindow, trayIconManager, mouseHandler);
                //notificationTrayIconWindow.Show(); //todo Проверить нужно ли показывать окно
                //notificationTrayIconWindow.Hide();
                return notificationTrayIconWindow;
            }
            private static Input CreateHadnlerInput(Dispatcher? uiDispatcher = null)
            {
                uiDispatcher ??= System.Windows.Application.Current.Dispatcher;

                Input? inputHendler = null;
                uiDispatcher.Invoke(() => inputHendler = new Input());
                return inputHendler ?? throw new NullReferenceException();
            }
            private static ShortcutsManager CreateShortcutsManager(IKeyboardCallback keyboardCallback, IEnumerable<IInvokeShortcuts> listFunc) => new ShortcutsManager(keyboardCallback, listFunc);
            private static MainWindowCommand CreateMainWindowCommand(Window window,MainWindowViewModel viewModel, IKeyboardHandler keyboardHandler) => new MainWindowCommand(window,viewModel, keyboardHandler);
        }
    }



}

