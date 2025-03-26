using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

using R3;

using Windows.Win32;
using Windows.Win32.Foundation;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal class ShellHookPriorityHandlers //todo кто Dispose
    {
        readonly ObserverExclusiveMode _observerExclusiveMode;
        readonly ObserverMsScreenClipExecuting _observerMsScreenClipExecuting;

        public ShellHookPriorityHandlers(
            ObserverExclusiveMode win32ObserverExclusiveMode, 
            ObserverMsScreenClipExecuting observerMsScreenClipExecuting)
        {
            _observerExclusiveMode = win32ObserverExclusiveMode;
            _observerMsScreenClipExecuting = observerMsScreenClipExecuting;
        }

        internal void ShellHookHandler(HSHELL wParam, ref nint lParam, ref bool handled)
        {
            switch(wParam)
            {
                case HSHELL.GETMINRECT:
                break;
                case HSHELL.WINDOWACTIVATED:
                break;
                case HSHELL.RUDEAPPACTIVATED:
                      if(lParam is not 0) _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.WINDOWREPLACING:
                break;
                case HSHELL.WINDOWREPLACED:
                break;
                case HSHELL.WINDOWCREATED:
                      _observerMsScreenClipExecuting.CheckAndSetStateMsScreenClipExecuting(ref lParam);
                break;
                case HSHELL.WINDOWDESTROYED:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.ACTIVATESHELLWINDOW:
                break;
                case HSHELL.TASKMAN:
                break;
                case HSHELL.REDRAW:
                break;
                case HSHELL.FLASH:
                break;
                case HSHELL.ENDTASK:
                break;
                case HSHELL.APPCOMMAND:
                break;
                case HSHELL.MONITORCHANGED:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.LANGUAGE:
                break;
                case HSHELL.SYSMENU:
                break;
                case HSHELL.ACCESSIBILITYSTATE:
                break;
                case HSHELL.APPCOMMAND_DELETE:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                case HSHELL.APPCOMMAND_DWM_FLIP3D:
                      _observerExclusiveMode.CheckAndSetStateExcusiveMode();
                break;
                default:
#if DEBUG
                      System.Diagnostics.Debugger.Break();
#endif
                break;
            }
        }
    }
    internal partial class WindowProxyHandler : IDisposable
    {
        private bool _isDisposed = false;
        private const long WS_POPUP = 0x80000000L;
        protected HwndSource _proxyInputHandlerWindow;
        protected readonly Dispatcher _dispatcher;
        internal WindowProxyHandler(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _dispatcher.Invoke(() =>
            {
                HwndSourceParameters configInitWindow = new HwndSourceParameters(name: $"{nameof(FVH)}.{nameof(WindowProxyHandler)}-{Path.GetRandomFileName}", width: 0, height: 0)
                {
                    WindowStyle = unchecked((int)WS_POPUP)
                };
                _proxyInputHandlerWindow = new HwndSource(configInitWindow);
            });
            ArgumentNullException.ThrowIfNull(_proxyInputHandlerWindow);
        }    
        public void Dispose()
        {
            Dispose(isManagedContext: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool isManagedContext)
        {
            if(_isDisposed) return;
            
            if(isManagedContext) _dispatcher.Invoke(() => _proxyInputHandlerWindow.Dispose());
                        
            _isDisposed = true;
        }
        protected void AddHandler(HwndSourceHook hwndSourceHook) => _dispatcher.Invoke(() => _proxyInputHandlerWindow.AddHook(hwndSourceHook));                       
        protected void RemoveHandler(HwndSourceHook hwndSourceHook) => _dispatcher.Invoke(() => _proxyInputHandlerWindow.RemoveHook(hwndSourceHook));                 
    }
    internal partial class Win32HookManager: WindowProxyHandler
    {
        private bool _isDisposed = false;
        private const int _errorCodeRegisterWindowMessage = 0;
        private readonly uint WM_SHELLHOOKMESSAGE;
        private readonly ShellHookPriorityHandlers _shellHookPriorityHandlers;
        internal Win32HookManager(Dispatcher dispatcher, ShellHookPriorityHandlers shellHookPriorityHandlers) : base(dispatcher)
        {
            _shellHookPriorityHandlers = shellHookPriorityHandlers;
            WM_SHELLHOOKMESSAGE = _dispatcher.Invoke(() => RegisterWindowMessageW("SHELLHOOK"));
            if(WM_SHELLHOOKMESSAGE is _errorCodeRegisterWindowMessage) throw new Win32Exception();
        }
        protected override void Dispose(bool isManagedContext)
        {
            if(_isDisposed is true) return;
            if(isManagedContext is true)
            {
                _ = DeregisterShellHookWindow(new HWND(_proxyInputHandlerWindow.Handle));
                base.RemoveHandler(ShellHookMessageWorker);
            }
            _isDisposed = true;
            base.Dispose(isManagedContext);
        }
        internal void RegisterShellHook() =>
        _proxyInputHandlerWindow.Dispatcher.Invoke(() =>
        {
            _proxyInputHandlerWindow.AddHook(ShellHookMessageWorker);
            HWND hwnd = new HWND(_proxyInputHandlerWindow.Handle);
            bool res = RegisterShellHookWindow(hwnd);
            if(res is false) throw new Win32Exception();
        });
        private nint ShellHookMessageWorker(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {            
            if(((uint)msg == WM_SHELLHOOKMESSAGE) is true) _shellHookPriorityHandlers.ShellHookHandler((HSHELL)wParam, ref lParam, ref handled);           
            return hwnd;
        }

     
        [DllImport("user32")]
        private static extern bool RegisterShellHookWindow(HWND hwnd);
        [DllImport("user32")]
        private static extern bool DeregisterShellHookWindow(HWND hwnd);
        /// <summary>
        /// Если сообщение успешно зарегистрировано, возвращаемое значение - идентификатор сообщения в диапазоне от 0xC000(49152) до 0xFFFF(65535).
        /// При неудачном выполнении функции возвращаемое значение равно нулю.
        /// </summary>
        [LibraryImport("user32")]
        private static partial uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)] string lpString);
        /// <summary>
        /// See description<see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow"> link HSHELL</see>.
        /// </summary>
    }
    internal enum HSHELL : uint
    {
        GETMINRECT = 5U,
        WINDOWACTIVATED = 4U,
        RUDEAPPACTIVATED = 32772U,
        WINDOWREPLACING = 14U,
        WINDOWREPLACED = 13U,
        WINDOWCREATED = 1U,
        WINDOWDESTROYED = 2U,
        ACTIVATESHELLWINDOW = 3U,
        TASKMAN = 7U,
        REDRAW = 6U,
        FLASH = 32774U,
        ENDTASK = 10U,
        APPCOMMAND = 12U,
        MONITORCHANGED = 16U,
        LANGUAGE = 8U,
        SYSMENU = 9U,
        ACCESSIBILITYSTATE = 11U,
        APPCOMMAND_DELETE = 53U,  // Происходит при появлении окна системного выбора окон => ALT + TAB, WIN + TAB
        APPCOMMAND_DWM_FLIP3D = 54U // Происходит при закрытии окна системного выбора окон => ALT + TAB, WIN + TAB
    }

    internal partial class ObserverExclusiveMode : IDisposable
    {
        private bool _isDispose = false;
        private bool _isExcusiveMode = false;
        internal Win32ExclusiveModeChecker _exclusiveModeChecker;
        internal readonly R3.BehaviorSubject<bool> ExcusiveMode;
        private readonly Dispatcher _dispatcher;
        internal ObserverExclusiveMode(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _exclusiveModeChecker = _dispatcher.Invoke(()=> _ = new Win32ExclusiveModeChecker());

            ExcusiveMode = new R3.BehaviorSubject<bool>(false);

            ArgumentNullException.ThrowIfNull(_exclusiveModeChecker);
        }
        public void Dispose()
        {
            if(_isDispose) return;
            _isDispose = true;
            ExcusiveMode.OnCompleted(Result.Success);
            ExcusiveMode.Dispose();
            _dispatcher.Invoke(() => _exclusiveModeChecker.Dispose());
        }
        internal void CheckAndSetStateExcusiveMode()
        {
            bool isExcusiveMode = false;
            if(ExcusiveMode.Value == false)
            {
                TimeSpan empiricalTimeoutSpinWait = TimeSpan.FromMilliseconds(25); // Предполагаемая задержка между получение фокуса окна и установкой режима
                _ = SpinWait.SpinUntil(() =>
                {
                    isExcusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher);
                    return isExcusiveMode is true;
                }, empiricalTimeoutSpinWait);
            }
            else { isExcusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher); }

            _isExcusiveMode = isExcusiveMode;
            if(_isExcusiveMode is true) { if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is false) _ = Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread(); }
            ExcusiveMode.OnNext(_isExcusiveMode);
        }                               
    }
    internal partial class ObserverMsScreenClipExecuting
    {
        private const string MsScreenClipPath = "C:\\Windows\\SystemApps\\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\\ScreenClippingHost.exe";
        private HashSet<Process> _msScreenClipExecutingSet;
        internal readonly R3.BehaviorSubject<bool> MsScreenClipExecuting;
        public ObserverMsScreenClipExecuting()
        {
            _msScreenClipExecutingSet = new HashSet<Process>();
            MsScreenClipExecuting = new BehaviorSubject<bool>(false);
        }
        internal void CheckAndSetStateMsScreenClipExecuting(ref nint handleWindow)
        {
            void ProcessExitedEvent(object? proc, EventArgs _)
            {
                if(proc is not Process pr) throw new InvalidCastException();
                pr.Exited -= ProcessExitedEvent;
                pr.Dispose();
                MsScreenClipExecuting.OnNext(false);
            }
            uint treadID = GetWindowThreadProcessId(new HWND(handleWindow), out uint procID);
            Process pr = System.Diagnostics.Process.GetProcessById((int)procID);
            if(pr.MainModule is null)
            {
                pr.Dispose();
                return;
            }
            if(pr.MainModule.FileName == MsScreenClipPath)
            {
                if(pr.HasExited is true) return;
                if(_msScreenClipExecutingSet.Contains(pr) is true) return;
                _ = _msScreenClipExecutingSet.Add(pr);
                pr.EnableRaisingEvents = true;

                pr.Exited += ProcessExitedEvent;

                MsScreenClipExecuting.OnNext(true);
            }
        }              
        [DllImport("user32")]
        private static extern uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId);
    }
}