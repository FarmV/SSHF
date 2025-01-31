using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Diagnostics;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FVH.SSHF.Infrastructure.Win32;
using FVH.Background.Input.Infrastructure.Interfaces;
using Windows.Win32;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using R3;


namespace FVH.SSHF
{

    internal partial class App
    {
        internal const string UIThreadName = "FVH Main Thread";
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        private const int _errorUnhandled = 100_001;
        private const int _errorCreateMutex = 100_002;
        private const int ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
        private static int _applicationExitCode = 0;
        private static Mutex? _mutexSingleInstance;
        private readonly IHost _program;
        private readonly IServiceProvider _serviceProvider;

        internal static bool DesignerMode = true;
#if DEBUG
        internal static TraceSwitch Trace;        
        internal static App? GetDEBUG { get; private set; }

        internal static Stopwatch Stopwatch = new Stopwatch();
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

        /// <summary>
        /// <see cref="Main"/>      
        /// </summary>
        [STAThread]
        private static int Main(string[]? args)
        {          
            if(CreateMutexForSingleProgram() is false) return _errorCreateMutex;
            
            _ = Native.SetPriorityClass(Native.GetCurrentProcess(), ABOVE_NORMAL_PRIORITY_CLASS);

            Thread.CurrentThread.Name = UIThreadName;

            System.Windows.Application application = new System.Windows.Application();
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
         
            WpfProviderInitializer.SetDefaultObservableSystem(EmergencyAppTermination, DispatcherPriority.Render, dispatcher);
           
            application.DispatcherUnhandledException += (object _, DispatcherUnhandledExceptionEventArgs ev) => { ev.Handled = true; EmergencyAppTermination(ev.Exception); };
           
            _ = Thread.CurrentThread.InThreadUITimeCriticalSection(); //инициализация статического конструктора


            //string mes = string.Empty;
            //AppNativeHelper.DebugExceptionFormat(ref mes, new StackTrace(true));
            //TimeoutException test = new TimeoutException(mes);


            /// <summary>
            /// Чтобы окно при вставке изображения из буфера обмена сохраняло пропорции и не масштабировалось. 
            /// Возвращаемое значение 2147508241 вероятно не корректно. SetThreadDpiAwarenessContext предполагает возврат nint, 
            /// из перечисления DPI_AWARENESS_CONTEXT прошлого состояния потока.
            /// </summary>
            if(Native.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero)
            {
                string error = Marshal.GetLastPInvokeErrorMessage();
                throw new InvalidOperationException(error);
            }
         
            Observable<StartupEventArgs> startupObservable = Observable.FromEvent<StartupEventHandler, StartupEventArgs>(
            (Action<StartupEventArgs> handler) => new StartupEventHandler((object _, StartupEventArgs ev) => handler(ev)),
            h => application.Startup += h,
            h => application.Startup -= h);

            IDisposable? disposableSubscribeStartup = null;

            disposableSubscribeStartup = startupObservable.ObserveOnCurrentDispatcher().
            SubscribeAwait(onNextAsync: async (StartupEventArgs _, CancellationToken _) =>
            {                
                await Start(args);
                disposableSubscribeStartup?.Dispose();
            });



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
        internal static void EmergencyAppTermination(Exception ex)
        {
#if DEBUG
            if(System.Diagnostics.Debugger.IsAttached)
            {
                string exMessage = ex.Message;
                string? stackTrace = ex.StackTrace;
                System.Diagnostics.Debugger.Break();
            }
#endif
            Application.Current.Shutdown(_errorUnhandled);
        }
        private static bool CreateMutexForSingleProgram()
        {
            bool mutexWasCreated;
            try { _mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { return false; }
            if(mutexWasCreated is false) return false;
            return true;
        }      
        private static partial class Native
        {
            [LibraryImport("user32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static partial int SetThreadDpiAwarenessContext(nint dpiContext);
            [LibraryImport("Kernel32")]
            [return: MarshalAs(UnmanagedType.SysInt)]
            internal static partial nint GetCurrentProcess();
            [LibraryImport("Kernel32")]
            [return: MarshalAs(UnmanagedType.U4)]
            internal static partial uint GetPriorityClass(nint hProcess);
            [LibraryImport("Kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool SetPriorityClass(nint hProcess, uint dwPriorityClass);
            [LibraryImport("Kernel32")]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static partial int GetThreadPriority(nint hThread);
            [LibraryImport("Kernel32")]
            [return: MarshalAs(UnmanagedType.SysInt)]
            internal static partial nint GetCurrentThread();
        }
    }
    internal static partial class AppNativeHelper
    {
        private static readonly Win32MMCSS myVar;
        private static Win32MMCSS Win32MMCSS => myVar;
        static AppNativeHelper()
        {
            if(Thread.CurrentThread.Name is not App.UIThreadName) throw new InvalidOperationException();
            myVar = new Win32MMCSS(Dispatcher.FromThread(Thread.CurrentThread));
        }
        internal static bool InThreadUITimeCriticalSection(this Thread _) => Win32MMCSS.InTimeCriticalSection;
        internal static bool StartTimeCriticalSectionUI(this Thread _) => Win32MMCSS.StartTimeCriticalSectionUI();
        internal static bool StopTimeCriticalSectionUI(this Thread _) => Win32MMCSS.StopTimeCriticalSectionUI();
        [Conditional("DEBUG")]
        internal static void DebugExceptionFormat(ref string messageEx, StackTrace stackTrace,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        {
            StackFrame? frame = stackTrace.FrameCount > 0 ? stackTrace.GetFrame(0) : null;
            System.Reflection.MethodBase? method = frame?.GetMethod();
            Type? declaringType = method?.DeclaringType;
            frame?.GetFileColumnNumber();

            if(messageEx == string.Empty) messageEx = "Отсутствует";

            string str = $"{Environment.NewLine}{Environment.NewLine}Тип: {declaringType?.FullName}{Environment.NewLine}Метод: {method}{Environment.NewLine}Строка: {lineNumber}{Environment.NewLine}Отступ: {frame?.GetFileColumnNumber()}{Environment.NewLine}Файл: {filePath}{Environment.NewLine}Сообщение: {messageEx}";
            messageEx = str;
        }
    }
}
    


