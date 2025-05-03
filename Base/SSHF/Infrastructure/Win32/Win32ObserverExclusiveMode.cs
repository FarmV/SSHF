using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

using R3;

using Windows.Win32;
using Windows.Win32.Foundation;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class Win32ObserverExclusiveMode : IDisposable
    {
        private bool _isDispose = false;
        private const long WS_POPUP = 0x80000000L;
        private const int _errorCodeRegisterWindowMessage = 0;
        private bool _isExcusiveMode = false;
        private readonly uint WM_SHELLHOOKMESSAGE;
        private HwndSource _proxyInputHandlerWindow;
        internal Win32ExclusiveModeChecker _exclusiveModeChecker;
        internal readonly R3.BehaviorSubject<bool> ExcusiveMode;
        internal Win32ObserverExclusiveMode(Dispatcher UIDispatcher)
        {
            UIDispatcher.Invoke(() =>
            {
                HwndSourceParameters configInitWindow = new HwndSourceParameters(name: $"ShellMessageExclusiveModeHandler-{Path.GetRandomFileName}", width: 0, height: 0)
                {
                    WindowStyle = unchecked((int)WS_POPUP)
                };
                _proxyInputHandlerWindow = new HwndSource(configInitWindow);
                _exclusiveModeChecker = new Win32ExclusiveModeChecker(); // Важно чтобы владельцем объекта был поток UI, COM должен владеть 1 поток
            });

            WM_SHELLHOOKMESSAGE = RegisterWindowMessageW("SHELLHOOK");
            if(WM_SHELLHOOKMESSAGE is _errorCodeRegisterWindowMessage) throw new Win32Exception();
            ExcusiveMode = new R3.BehaviorSubject<bool>(false);


            ArgumentNullException.ThrowIfNull(_proxyInputHandlerWindow);
            ArgumentNullException.ThrowIfNull(_exclusiveModeChecker);
        }
        public void Dispose()
        {
            if(_isDispose) return;
            _isDispose = true;
            bool resultDeregisterShellHookWindow = DeregisterShellHookWindow(new HWND(_proxyInputHandlerWindow.Handle));
#if DEBUG
            #region DEBUG
            if(App.Trace.Level is not TraceLevel.Off)
            {
                if(resultDeregisterShellHookWindow is false)
                {
                    if(App.Trace.Level >= TraceLevel.Error) Debug.WriteLine(
                    message: $"{nameof(resultDeregisterShellHookWindow)}, TraceLevel - {TraceLevel.Error} => {nameof(resultDeregisterShellHookWindow)} = {resultDeregisterShellHookWindow}",
                    category: $"{typeof(Win32ObserverExclusiveMode)}.{nameof(Dispose)}");
                }
                else
                {

                    if(App.Trace.Level >= TraceLevel.Info) Debug.WriteLine(
                    message: $"{nameof(resultDeregisterShellHookWindow)}, TraceLevel - {TraceLevel.Info} => {nameof(resultDeregisterShellHookWindow)} = {resultDeregisterShellHookWindow}",
                    category: $"{typeof(Win32ObserverExclusiveMode)}.{nameof(Dispose)}");
                }
            }
            #endregion
#endif
            _proxyInputHandlerWindow?.RemoveHook(ShellHookMessageWorker);
            _proxyInputHandlerWindow?.Dispose();
            ExcusiveMode.OnCompleted(Result.Success);
            ExcusiveMode.Dispose();
            _exclusiveModeChecker.Dispose();
        }
        private bool GetCurrentStatusExcusiveMode() => _exclusiveModeChecker.CheckExclusiveMode(_proxyInputHandlerWindow.Dispatcher);
        private void CheckAndSetStateExcusiveMode()
        {
            bool isExcusiveMode = false;
            if(ExcusiveMode.Value == false)
            {
                TimeSpan empiricalTimeoutSpinWait = TimeSpan.FromMilliseconds(25); // Необходимо выполнить прокрутки в N-ое время. Предполагаемая задержка между получение фокуса окна и установкой режима
                SpinWait.SpinUntil(() =>
                {
                    isExcusiveMode = GetCurrentStatusExcusiveMode();
                    return isExcusiveMode is true;
                }, empiricalTimeoutSpinWait);
            }
            else
            {
                isExcusiveMode = GetCurrentStatusExcusiveMode();
            }

            _isExcusiveMode = isExcusiveMode;
            if(_isExcusiveMode is true)
            {
              if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is false) Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread();
            }
            ExcusiveMode.OnNext(_isExcusiveMode);
        }
        internal void RegisterShellHook() =>
        _proxyInputHandlerWindow.Dispatcher.Invoke(() =>
        {
           _proxyInputHandlerWindow.AddHook(ShellHookMessageWorker);
            nint hwnd = _proxyInputHandlerWindow.Handle;
            bool res = RegisterShellHookWindow(hwnd);
            if(res is false) throw new Win32Exception();
        });
        private nint ShellHookMessageWorker(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            //todo логирование и отладка

            if(((uint)msg == WM_SHELLHOOKMESSAGE) is true)
            {
                HSHELL typeMessage = (HSHELL)wParam;
                switch(typeMessage)
                {
                    case HSHELL.GETMINRECT:
                    break;
                    case HSHELL.WINDOWACTIVATED:
                    break;
                    case HSHELL.RUDEAPPACTIVATED:
                         if(lParam == 0) break;
                         CheckAndSetStateExcusiveMode();
                    break;
                    case HSHELL.WINDOWREPLACING:
                    break;
                    case HSHELL.WINDOWREPLACED:
                    break;
                    case HSHELL.WINDOWCREATED:

                    break;
                    case HSHELL.WINDOWDESTROYED:
                         CheckAndSetStateExcusiveMode();
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
                         CheckAndSetStateExcusiveMode();
                    break;
                    case HSHELL.LANGUAGE:
                    break;
                    case HSHELL.SYSMENU:
                    break;
                    case HSHELL.ACCESSIBILITYSTATE:
                    break;
                    case HSHELL.APPCOMMAND_DELETE:
                         CheckAndSetStateExcusiveMode();
                    break;
                    case HSHELL.APPCOMMAND_DWM_FLIP3D:
                         CheckAndSetStateExcusiveMode();
                    break;
                    default:
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                    break;
                }
            }
            return hwnd;
        }
        [LibraryImport("user32")]
        [return:MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterShellHookWindow(nint hwnd);
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeregisterShellHookWindow(nint hwnd);
        /// <summary>
        /// Если сообщение успешно зарегистрировано, возвращаемое значение - идентификатор сообщения в диапазоне от 0xC000(49152) до 0xFFFF(65535).
        /// При неудачном выполнении функции возвращаемое значение равно нулю.
        /// </summary>
        [LibraryImport("user32")]
        private static partial uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)]string lpString);
        /// <summary>
        /// See description<see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow"> link HSHELL</see>.
        /// </summary>
        private enum HSHELL : uint
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

        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, nint handle2, int x, int y, int cx, int cy, int flag);
        [LibraryImport("user32")]
        private static partial uint GetWindowThreadProcessId(nint hWnd,out uint lpdwProcessId);
    }
}
