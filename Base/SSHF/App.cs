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
using System.Windows;
using System.Reactive.Disposables;


namespace FVH.SSHF
{
    internal partial class App
    {
        internal static bool DesignerMode = true;
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        private const int _errorCreateMutex = 100501;
        private static int _applicationExitCode = 0;
        private readonly IHost _host;
        private IServiceProvider _serviceProvider;
        private static Mutex? _mutexSingleInstance;
        private readonly BasicDependencies _basicDependencies;
        private App(IHost host, BasicDependencies basicDependencies)
        {
            _host = host;
            _basicDependencies = basicDependencies;
            _serviceProvider = _host.Services;
        }
        private Dependency GetDependency<Dependency>() where Dependency : notnull => _serviceProvider.GetRequiredService<Dependency>();
        internal static StreamResourceInfo GetResource(Uri uriResource) => System.Windows.Application.GetResourceStream(uriResource);
        [STAThread]
        private static int Main(string[]? args)
        {
            if(CreateMutexForSingleProgram() is false) return _errorCreateMutex;

            Thread.CurrentThread.Name = "FVH Main Thread";

            /// <summary>
            /// Чтобы окно при вставке изображения из буфера обмена сохраняло пропорции и не масштабировалось. 
            /// Возвращаемое значение 2147508241 вероятно не корректно. SetThreadDpiAwarenessContext предполагает возврат nint, 
            /// из перечисления DPI_AWARENESS_CONTEXT прошлого состояния потока.
            /// </summary>
            if(SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero) 
            { 
                string error = Marshal.GetLastPInvokeErrorMessage();
                throw new InvalidOperationException(error);
            }

            System.Windows.Application application = new System.Windows.Application();
            application.Startup += (_,_) => Start(args);
            application.Run();
            return _applicationExitCode;
        }
        private static void Start(string[]? args)
        {
            DesignerMode = false;

            BasicDependencies basicDependencies = new BasicDependencies();
            IHost dependencies = basicDependencies.ConfigureDependencies(Thread.CurrentThread, args);

            App app = new App(dependencies, basicDependencies);
            app._serviceProvider = app._host.Services;
            app.RegShortcuts();
            System.Windows.Application.Current.Exit += app.Shutdown;
            app._host.Start();
        }
        private void Shutdown(object _, ExitEventArgs e)
        {
            _applicationExitCode = e.ApplicationExitCode;
            _basicDependencies.Dispose();
            _mutexSingleInstance?.Dispose();
            _host.Dispose();
        }
        private static bool CreateMutexForSingleProgram()
        {
            bool mutexWasCreated;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { return false; }
            if(mutexWasCreated is false) return false;
            return true;
        }
        private void RegShortcuts() => GetDependency<ShortcutsProvider>().RegisterShortcuts();
        [LibraryImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static partial int SetThreadDpiAwarenessContext(nint dpiContext);
    }
}

