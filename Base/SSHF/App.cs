using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Diagnostics;

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
    
namespace FVH.SSHF
{
    internal partial class App
    {
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private const string UIThreadName = "FVH Main Thread";
        private const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        private const int _errorCreateMutex = 100_001;
        private static int _applicationExitCode = 0;
        private static Mutex? _mutexSingleInstance;
        private readonly IHost _program;
        private readonly IServiceProvider _serviceProvider;

        internal static bool DesignerMode = true;
     #if DEBUG
        internal static TraceSwitch Trace;        
        internal static App? GetDEBUG { get; private set; }
        static App() => Trace = new TraceSwitch("Debug", "Debugging only") { Level = TraceLevel.Verbose };
     #endif
        private App(IHost program)
        {
            _program = program;
            _serviceProvider = _program.Services;
         #if DEBUG
            GetDEBUG = this;
         #endif
        }
     #if DEBUG
        internal Dependency GetDEBUGDependency<Dependency>() where Dependency : notnull => _serviceProvider.GetRequiredService<Dependency>();
     #endif
        internal static StreamResourceInfo GetResource(Uri uriResource) => System.Windows.Application.GetResourceStream(uriResource);
        [STAThread]
        private static int Main(string[]? args)
        {           
            if(CreateMutexForSingleProgram() is false) return  _errorCreateMutex;

            Thread.CurrentThread.Name = UIThreadName;

            System.Windows.Application application = new System.Windows.Application();
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            dispatcher.Invoke(() => RxApp.MainThreadScheduler = DispatcherScheduler.Current);

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
         
            IDisposable? disposableSubscribeStartup = null;
            disposableSubscribeStartup = application.Events().Startup.SelectMany(_ = Observable.FromAsync(actionAsync: async () => 
            await dispatcher.InvokeAsync<Task>(async () => await Start(args)).Task.Unwrap())).ObserveOn(RxApp.MainThreadScheduler).
            Subscribe(onNext: _ => disposableSubscribeStartup?.Dispose(), onError: ex => throw ex);
          
            _ = application.Run();

            return _applicationExitCode;
        }
        private static async Task Start(string[]? args)
        {       
            Thread uiThread = Thread.CurrentThread;

            async Task StartAsync()
            {              
                IHost thisProgram = await BasicDependencies.ConfigureDependencies(uiThread, args).ConfigureAwait(false);

                App app = new App(thisProgram);
   
                Dispatcher.FromThread(uiThread).Invoke(() => System.Windows.Application.Current.Exit += app.Shutdown);

                await thisProgram.Services.GetRequiredService<FastWindowManager>().CreateMainWindow().ConfigureAwait(false);
                await app._program.StartAsync().ConfigureAwait(false);
            }

            await Task.Run(StartAsync).ConfigureAwait(false);         
        }
        private void Shutdown(object _, ExitEventArgs e)
        {
            _applicationExitCode = e.ApplicationExitCode;            
            _program.StopAsync().GetAwaiter().GetResult();          
            _mutexSingleInstance?.Dispose();
        }
        private static bool CreateMutexForSingleProgram()
        {
            bool mutexWasCreated;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { return false; }
            if(mutexWasCreated is false) return false;
            return true;
        }      
        [LibraryImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static partial int SetThreadDpiAwarenessContext(nint dpiContext);
    }
}

