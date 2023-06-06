using FVH.Background.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SSHF.Infrastructure;
using SSHF.ViewModels;
using SSHF.ViewModels.MainWindowViewModel;



using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Resources;

namespace SSHF
{
    internal partial class App
    {
        internal static bool DesignerMode = true;
        internal static event EventHandler? DpiChange;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private readonly CancellationTokenSource _tokenShutdownHost;
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
        private void RegShortcuts() => GetDependencie<ShortcutsProvider>().RegisterShortcuts();
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
            if (_host.Services.GetService<WPFDropImageFile>() is WPFDropImageFile setImage) setImage.Dispose();
            _mutexSingleInstance?.ReleaseMutex();
            _mutexSingleInstance?.Close();
            if (_host.Services.GetService<TrayIconManager>() is TrayIconManager notificator) await notificator.DisposeAsync();
        }
        internal static StreamResourceInfo GetResource(Uri uriResource) => System.Windows.Application.GetResourceStream(uriResource);
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
    }



}

