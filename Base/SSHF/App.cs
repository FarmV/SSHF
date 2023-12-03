using FVH.Background.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SSHF.Infrastructure;
using SSHF.Infrastructure.TrayIconManagment;



using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Resources;

namespace SSHF
{
    internal partial class App
    {
        internal static bool DesignerMode = true;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private readonly CancellationTokenSource _tokenShutdownHost;
        private readonly IHost _host;
        private IServiceProvider _serviceProvider;
        private static Mutex? _mutexSingleInstance;
        private static nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new nint(-4);
        private const int _errorCreatemutex = -100501;
        private Dependencie GetDependencie<Dependencie>() where Dependencie : notnull => _serviceProvider.GetRequiredService<Dependencie>();
        private App(IHost host)
        {
            _tokenShutdownHost = new CancellationTokenSource();
            _host = host;
            _serviceProvider = _host.Services;
        }
        internal static StreamResourceInfo GetResource(Uri uriResource) => System.Windows.Application.GetResourceStream(uriResource);
        [STAThread]
        private static void Main(string[] args)
        {
            bool mutexWasCreated = false;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { Environment.Exit(_errorCreatemutex); }
            if (mutexWasCreated is false) Environment.Exit(_errorCreatemutex);
            
            Thread.CurrentThread.Name = "FVH Main Thread";

            if (SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero) // чтобы окно при вставке изображение из буфера обмена сохраняло пропорции и не масштабировалось
            {
                string error = Marshal.GetLastPInvokeErrorMessage();
                throw new InvalidOperationException(error);
            }

            System.Windows.Application application = new System.Windows.Application();
            application.Startup += async (_, _) => await Start(args).ConfigureAwait(false);
            application.Run();
        }
        private static async Task Start(string[] args)
        {
            DesignerMode = false;
         
            App app = new App(BasicDependencies.ConfigureDependencies(Thread.CurrentThread));
            app._serviceProvider = app._host.Services;
            app.RegShortcuts();
            System.Windows.Application.Current.Exit += (_, _) => app.Shutdown();
            await app._host.StartAsync();
        }
        private void RegShortcuts() => GetDependencie<ShortcutsProvider>().RegisterShortcuts();
        [DllImport("user32", SetLastError = true)]
        private static extern uint SetThreadDpiAwarenessContext(nint dpiContext);
        private void Shutdown()
        {
            _tokenShutdownHost?.Cancel();
            if (_host.Services.GetService<Input>() is Input input) input.Dispose();
            if (_host.Services.GetService<WPFDropImageFile>() is WPFDropImageFile setImage) setImage.Dispose();
            _mutexSingleInstance?.ReleaseMutex();
            _mutexSingleInstance?.Close();
            if (_host.Services.GetService<TraiIcon>() is TraiIcon notificator) notificator.Dispose();
        }
    }
}

