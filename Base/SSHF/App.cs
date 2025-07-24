using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;

using R3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using FVH.SSHF.Infrastructure.Win32;


namespace FVH.SSHF
{
    internal partial class App
    {
        private const int ErrorUnhandled = 100_001;
        private const int ErrorCreateMutex = 100_002;
        private const int ErrorTimeoutBaseApp = 100_003;
        private volatile static int s_applicationExitCode = 0;
        private static Mutex? s_mutexSingleInstance;
        private readonly IHost _program;
        private readonly IServiceProvider _serviceProvider;
        internal const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (nint)(-4);
        internal const string UiThreadName = "FVH Main Thread";
        internal volatile static bool DesignerMode = true;
#if DEBUG
        internal static TraceSwitch Trace;        
        internal static App? GetDEBUG { get; private set; }
        internal static Stopwatch Stopwatch = new Stopwatch();
        static App() => Trace = new TraceSwitch("Debug", "Debugging only") { Level = TraceLevel.Off };
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

        static partial void ExtensionStartLogic(string[]? args);

        [STAThread]
        private static void Main(string[]? args)
        {
            args ??= [];

            ExtensionStartLogic(args);

            if(CreateMutexForSingleProgram() is false) { Environment.ExitCode = ErrorCreateMutex; return; }
           
            _ = Native.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); // обязательно до new System.Windows.Application(); иначе контекст сбрасывается

            const int ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
            _ = Native.SetPriorityClass(Native.GetCurrentProcess(), ABOVE_NORMAL_PRIORITY_CLASS);

            Thread.CurrentThread.Name = UiThreadName;

            System.Windows.Application application = new System.Windows.Application();
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            application.Startup += ApplicationStartupEvent;

            _ = application.Run();
            Environment.ExitCode = s_applicationExitCode;
        }
        private static void ApplicationStartupEvent(object sender, StartupEventArgs e)
        {          
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            WpfProviderInitializer.SetDefaultObservableSystem(EmergencyAppTermination, DispatcherPriority.Send, dispatcher);
            Application.Current.DispatcherUnhandledException += (object _, DispatcherUnhandledExceptionEventArgs ev) => { ev.Handled = true; EmergencyAppTermination(ev.Exception); };
            AppDomain.CurrentDomain.UnhandledException += (_, e) => EmergencyAppTermination((Exception)e.ExceptionObject);
            RuntimeHelpers.RunClassConstructor(typeof(AppHelper).TypeHandle); // Инициализация статического конструктора

            _ = Start(e.Args).ContinueWith((Task task) => EmergencyAppTermination(task.Exception!), TaskContinuationOptions.OnlyOnFaulted);
        }
        private static async Task Start(string[]? args)
        {
            DesignerMode = false;
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
            s_applicationExitCode = e.ApplicationExitCode;            
            _program.StopAsync().GetAwaiter().GetResult();          
            s_mutexSingleInstance?.Dispose();
        }
        internal static void EmergencyAppTermination(Exception ex)
        {
            s_applicationExitCode = ErrorUnhandled;
            Application.Current.Shutdown(ErrorUnhandled);
#if DEBUG
            Debug.WriteLine($"{Environment.NewLine}{ex.StackTrace}");
            Type typeEx = ex.GetType();

            Debug.WriteLine($"{Environment.NewLine}{typeEx.FullName}");
            Debug.WriteLine(ex.Message);
#endif
        }
        private static bool CreateMutexForSingleProgram()
        {
            const string MutexNameSingleInstance = "FVH.SSHF.SingleProgramInstance";
            bool mutexWasCreated;
            try { s_mutexSingleInstance = new Mutex(true, MutexNameSingleInstance, out mutexWasCreated); }
            catch { return false; }
            if(mutexWasCreated is false) return false;
            return true;
        }      
        internal static partial class Native
        {
            [LibraryImport("user32", SetLastError = true)]
            internal static partial nint SetThreadDpiAwarenessContext(nint dpiContext);
            [LibraryImport("user32", SetLastError = true)]
            internal static partial nint GetThreadDpiAwarenessContext();
            [LibraryImport("kernel32")]
            internal static partial nint GetCurrentProcess();
            [LibraryImport("kernel32")]
            internal static partial uint GetPriorityClass(nint hProcess);
            [LibraryImport("kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static partial bool SetPriorityClass(nint hProcess, uint dwPriorityClass);
            [LibraryImport("kernel32")]
            internal static partial int GetThreadPriority(nint hThread);
            [LibraryImport("kernel32")]
            internal static partial nint GetCurrentThread();
        }
    } 
    internal static partial class AppHelper
    {
        private static readonly Win32MMCSS win32MMCSS;
        private static Win32MMCSS Win32MMCSS => win32MMCSS;
        static AppHelper()
        {
            if(Thread.CurrentThread.Name is not App.UiThreadName) throw new InvalidOperationException();
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
#if DEBUG
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
        [Conditional("DEBUG")]
        private static void GetMessageNTSTATUSEx(uint ntStatusValue, ref string? messageEx)
        {
            const string ModuleNameTableErrorCodes = "ntdll";
            nint handle = GetModuleHandleW(ModuleNameTableErrorCodes);

            _ = FormatMessageW(
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                 FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_HMODULE |
                  FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
                   handle,
                    ntStatusValue,
                     0 /*Язык по умолчанию*/,
                       out nint lpBuffer,
                       0 /*Минимальный размер(не используется с FORMAT_MESSAGE_ALLOCATE_BUFFER*//*,*/
                        /* Аргументы для вставки (не используются)*/);

            messageEx = Marshal.PtrToStringUni(lpBuffer)!;

            _ = LocalFree(lpBuffer);
        }
        [LibraryImport("Kernel32")]
        private static partial nint LocalFree(nint hMem);
        [LibraryImport("Kernel32")]
        private static partial nint GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        [LibraryImport("Kernel32")]
        private static partial uint FormatMessageW(FORMAT_MESSAGE_OPTIONS dwFlags, nint lpSource, uint dwMessageId, uint dwLanguageId, out nint lpBuffer, uint nSize, [Optional] nint Arguments);
        [Flags]
        internal enum FORMAT_MESSAGE_OPTIONS : uint
        {
            FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
            FORMAT_MESSAGE_FROM_STRING = 0x00000400,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        }
#endif
    }
}   