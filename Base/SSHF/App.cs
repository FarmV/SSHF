using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R3;

using FVH.SSHF.Infrastructure.Win32;
using FVH.SSHF.FastWindowArea;

namespace FVH.SSHF
{

    internal partial class App
    {
        internal const string UIThreadName = "FVH Main Thread";
        private const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
        private const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        private const int ERROR_UNHANDLED = 100_001;
        private const int ERROR_CREATE_MUTEX = 100_002;
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

        [STAThread]
        private static void Main(string[]? args)
        {          
            if(CreateMutexForSingleProgram() is false)
            {
                Environment.ExitCode = ERROR_CREATE_MUTEX;
                return;
            }
            
            _ = Native.SetPriorityClass(Native.GetCurrentProcess(), ABOVE_NORMAL_PRIORITY_CLASS);

            Thread.CurrentThread.Name = UIThreadName;

            System.Windows.Application application = new System.Windows.Application();
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
         
            WpfProviderInitializer.SetDefaultObservableSystem(EmergencyAppTermination, DispatcherPriority.Send, dispatcher);
           

            application.DispatcherUnhandledException += (object _, DispatcherUnhandledExceptionEventArgs ev) => { ev.Handled = true; EmergencyAppTermination(ev.Exception); };
            AppDomain.CurrentDomain.UnhandledException += (_,e) => EmergencyAppTermination((Exception)e.ExceptionObject);

           
            _ = Thread.CurrentThread.InUIThreadTimeCriticalSection(); //инициализация статического конструктора

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

            Environment.ExitCode = _applicationExitCode;
        }
        private static async Task Start(string[]? args)
        {       
            Thread uiThread = Thread.CurrentThread;

            async Task StartAsync()
            {              
                IHost thisProgram = await BasicDependencies.ConfigureDependencies(uiThread, args).ConfigureAwait(false);

                App app = new App(thisProgram);

                Dispatcher.FromThread(uiThread).Invoke(() => System.Windows.Application.Current.Exit += app.Shutdown);
           
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
            _applicationExitCode = ERROR_UNHANDLED;
            Application.Current.Shutdown(ERROR_UNHANDLED);
#if DEBUG
            Debug.WriteLine($"{Environment.NewLine}{ex.StackTrace}");
            Type typeEx = ex.GetType();

            Debug.WriteLine($"{Environment.NewLine}{typeEx.FullName}");
            Debug.WriteLine(ex.Message);
#endif
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
    internal static partial class AppHelper
    {
        private static readonly Win32MMCSS win32MMCSS;
        private static Win32MMCSS Win32MMCSS => win32MMCSS;
        static AppHelper()
        {
            if(Thread.CurrentThread.Name is not App.UIThreadName) throw new InvalidOperationException();
            win32MMCSS = new Win32MMCSS(Dispatcher.FromThread(Thread.CurrentThread));
        }
        internal static bool InUIThreadTimeCriticalSection(this Thread _) => Win32MMCSS.InTimeCriticalSection;
        internal static bool InUIThreadTimeCriticalSection(this SynchronizationContext? _) => Win32MMCSS.InTimeCriticalSection;
        internal static bool StartUITimeCriticalSectionThrowIfNotUIThread(this Thread _) => Win32MMCSS.StartTimeCriticalSectionUI();
        internal static bool StopUITimeCriticalSectionThrowIfNotUIThread(this Thread _) => Win32MMCSS.StopTimeCriticalSectionUI();

        internal static bool StartSafeUITimeCriticalSection(this SynchronizationContext? _)
        {
            bool res = false;
            if(System.Windows.Application.Current.Dispatcher.CheckAccess() is false) res = System.Windows.Application.Current.Dispatcher.Invoke(() => Win32MMCSS.StartTimeCriticalSectionUI());
            else res = Win32MMCSS.StartTimeCriticalSectionUI();
            return res;
        }
        internal static bool StopSafeUITimeCriticalSection(this SynchronizationContext? _)
        {
            bool res = false;
            if(System.Windows.Application.Current.Dispatcher.CheckAccess() is false) res = System.Windows.Application.Current.Dispatcher.Invoke(() => Win32MMCSS.StopTimeCriticalSectionUI());
            else res = Win32MMCSS.StopTimeCriticalSectionUI();
            return res;
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DebugExceptionFormat(ref string messageEx, StackTrace stackTrace,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        {
            StackFrame? frame = stackTrace.FrameCount > 0 ? stackTrace.GetFrame(0) : null;
            System.Reflection.MethodBase? method = frame?.GetMethod();
            Type? declaringType = method?.DeclaringType;
            _ = frame?.GetFileColumnNumber();

            if(messageEx == string.Empty) messageEx = "Отсутствует";

            string str = $"{Environment.NewLine}Сообщение: {messageEx}{Environment.NewLine}{Environment.NewLine}Тип: {declaringType?.FullName}{Environment.NewLine}Метод: {method}{Environment.NewLine}Строка: {lineNumber}{Environment.NewLine}Отступ: {frame?.GetFileColumnNumber()}{Environment.NewLine}Файл: {filePath}{Environment.NewLine}";
            messageEx = str;

            //string message = "It's test message";
            //AppHelper.DebugExceptionFormat(ref message, new StackTrace());
            //TimeoutException Test = new TimeoutException(message); FormatEx
        }
    }
}   