using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using FVH.Background.Input;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.TrayIconManagment;


namespace FVH.SSHF
{
    internal partial class App
    {
        internal static bool DesignerMode = true;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        private const int _errorCreateMutex = -100501;
        private readonly IHost _host;
        private IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _tokenShutdownHost;
        private static Mutex? _mutexSingleInstance;
        private App(IHost host)
        {
            _tokenShutdownHost = new CancellationTokenSource();
            _host = host;
            _serviceProvider = _host.Services;
        }
        private Dependency GetDependency<Dependency>() where Dependency : notnull => _serviceProvider.GetRequiredService<Dependency>();
        internal static StreamResourceInfo GetResource(Uri uriResource) => System.Windows.Application.GetResourceStream(uriResource);
        [STAThread]
        private static void Main(string[]? args)
        {
            bool mutexWasCreated = false;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { Environment.Exit(_errorCreateMutex); }
            if (mutexWasCreated is false) Environment.Exit(_errorCreateMutex);

            Thread.CurrentThread.Name = "FVH Main Thread";

            if (SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero) // чтобы окно при вставке изображения из буфера обмена сохраняло пропорции и не масштабировалось
            { // return 2147508241 вероятно возвращаемое значение не корректно, SetThreadDpiAwarenessContext должен возращать nint, из перечислеия DPI_AWARENESS_CONTEXT прошлого состояния потока
                string error = Marshal.GetLastPInvokeErrorMessage();
                throw new InvalidOperationException(error);
            }

            System.Windows.Application application = new System.Windows.Application();
            application.Startup += async (_, _) => await Start(args).ConfigureAwait(false);
            application.Run();
        }
        private static async Task Start(string[]? args)
        {
            DesignerMode = false;

            App app = new App(BasicDependencies.ConfigureDependencies(Thread.CurrentThread, args));
            app._serviceProvider = app._host.Services;
            app.RegShortcuts();
            System.Windows.Application.Current.Exit += (_, _) => app.Shutdown();
            await app._host.StartAsync();
        }
        private void RegShortcuts() => GetDependency<ShortcutsProvider>().RegisterShortcuts();
        [LibraryImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static partial int SetThreadDpiAwarenessContext(nint dpiContext);
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

