using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class HookManager: WindowProxyHandler
    {
        private bool _isDisposed = false;
        private const int ErrorCodeRegisterWindowMessage = 0;
        private readonly uint WM_SHELLHOOKMESSAGE;
        private readonly ShellHookPriorityHandlers _shellHookPriorityHandlers;
        internal HookManager(Dispatcher dispatcher, ShellHookPriorityHandlers shellHookPriorityHandlers) : base(dispatcher)
        {
            _shellHookPriorityHandlers = shellHookPriorityHandlers;
            WM_SHELLHOOKMESSAGE = _dispatcher.Invoke(() => RegisterWindowMessageW("SHELLHOOK"));
            if(WM_SHELLHOOKMESSAGE is ErrorCodeRegisterWindowMessage) throw new Win32Exception();
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
}