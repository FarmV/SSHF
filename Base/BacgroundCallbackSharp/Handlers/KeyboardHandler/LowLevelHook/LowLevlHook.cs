using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal partial class LowLevelKeyHook : CriticalFinalizerObject, IDisposable
    {
        internal event EventHandler<EventKeyLowLevelHook>? KeyDownEvent;
        internal event EventHandler<EventKeyLowLevelHook>? KeyUpEvent;
        /// <summary>
        /// Поддержка по умолчанию для обратных вызовов есть только для WH_KEYBOARD_LL и WH_MOUSE_LL
        /// </summary>
        private const uint dwThreadIdAllInTheSameDesktop = 0;
        private const int WH_KEYBOARD_LL = 13;
        private bool _disposed = false;
        private nint _hookID = nint.Zero;
        private delegate nint KeyboardHookHandler(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam);
        private EventKeyLowLevelHook? _eventDownLastKey;
        private EventKeyLowLevelHook? _eventUpLastKey;
        private KeyboardHookHandler? hookHandler;
        public LowLevelKeyHook() { }
        public void Dispose()
        {
            if (_disposed is true) return;
            UninstallHook();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        ~LowLevelKeyHook()
        {
            try
            {
                if (_disposed is true) return;
                UnhookWindowsHookEx(_hookID);
                Marshal.FreeHGlobal(_hookID);
            }
            catch { }
        }
        public void InstallHook()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            hookHandler = HookFunc;
            _hookID = SetHook(hookHandler);
        }
        public void UninstallHook() 
        {
            if (_disposed is true) return;
            UnhookWindowsHookEx(_hookID); 
        }
        private nint SetHook(KeyboardHookHandler proc) 
        {
            if (Process.GetCurrentProcess().MainModule is not ProcessModule module) throw new NullReferenceException(nameof(module));
            nint hMod = GetModuleHandleW(module.ModuleName);
            if(hMod == nint.Zero) throw new NullReferenceException(nameof(hMod));

            nint handleHookProcedure = SetWindowsHookEx(WH_KEYBOARD_LL, proc, hMod, dwThreadIdAllInTheSameDesktop);
            if(handleHookProcedure == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(),$"{nameof(handleHookProcedure)}{Environment.NewLine}{Environment.NewLine}{Marshal.GetLastPInvokeErrorMessage()}");
            }
            return handleHookProcedure;
        }
        private nint HookFunc(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam)
        {
            if (nCode is 0)
            {
                if (KeyDownEvent is null & KeyUpEvent is null) CallNextHookEx(_hookID, nCode, wParam, lParam);
               
                if (wParam is WMEvent.WM_KEYDOWN || wParam is WMEvent.WM_SYSKEYDOWN)
                {                    
                    _eventDownLastKey = new EventKeyLowLevelHook(lParam.VKCode);
                    KeyDownEvent?.Invoke(this, _eventDownLastKey);
                    if (_eventDownLastKey.Break is true)
                    {
                        _eventDownLastKey.Break = false;
                        _eventDownLastKey = null;
                        return (nint)1;
                    }
                }

                if (wParam is WMEvent.WM_KEYUP || wParam is WMEvent.WM_SYSKEYUP)
                {
                    if (KeyUpEvent is null) CallNextHookEx(_hookID, nCode, wParam, lParam);

                    _eventUpLastKey = new EventKeyLowLevelHook(lParam.VKCode);

                    KeyUpEvent?.Invoke(this, _eventUpLastKey);
                    _eventUpLastKey = null;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}

