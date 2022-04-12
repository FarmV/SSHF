using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Threading;

using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;

namespace SSHF.Infrastructure.Algorithms.Input.Keybord.Base
{


    internal class MyLowlevlhook : IAsyncDisposable
    {

        public async ValueTask DisposeAsync() => await Task.Run(() => { UninstallHook(); GC.SuppressFinalize(this); });

        private delegate IntPtr KeyboardHookHandler(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam);
        private KeyboardHookHandler? hookHandler;

        private IntPtr hookID = IntPtr.Zero;

        public void InstallHook()
        {
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);

            // Thread.CurrentThread.Name = "Тест creacte InstallHook";
            // System.Windows.Forms.Application.Run();       // =(
            // Dispatcher.Run();                              // =(

        }



        ~MyLowlevlhook() => UninstallHook();

        public void UninstallHook() => UnhookWindowsHookEx(hookID);


        private readonly int WH_KEYBOARD_LL = 13;//
        private IntPtr SetHook(KeyboardHookHandler proc) => SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                 GetModuleHandleW(Process.GetCurrentProcess().MainModule is not ProcessModule module2 ? throw new NullReferenceException() : module2.ModuleName ?? throw new NullReferenceException()), 0);


        internal delegate void KeyboardHookCallback(VKeys key, SettingHook setting);
        internal event KeyboardHookCallback? KeyDown;
        //   internal event KeyboardHookCallback? KeyUp;

        internal SettingHook Settings = new SettingHook();
        private int Count = default;
        private IntPtr HookFunc(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam)
        {
            if (Count < 0 || Count > 1) throw  new InvalidOperationException();
            if (nCode >= 0)
            {
            
                if ((wParam is WMEvent.WM_KEYUP || wParam is WMEvent.WM_SYSKEYUP) & Count is 1)
                {
                    Count--;
                    return (System.IntPtr)1; 
                }

                if ( wParam is WMEvent.WM_KEYDOWN || wParam is WMEvent.WM_SYSKEYDOWN ) 
                {
                    KeyDown?.Invoke(lParam.Vkcode, Settings);
                    if (Settings.Break is true)
                    {
                        Settings.Break = false;
                        Count++;
                        return (System.IntPtr)1;
                    }
                }

            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        enum WMEvent
        {
            WM_KEYDOWN = 256,
            WM_SYSKEYDOWN = 260,
            WM_KEYUP = 257,
            WM_SYSKEYUP = 261
        }
        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN
        [StructLayout(LayoutKind.Sequential)]
        private struct TagKBDLLHOOKSTRUCT
        {
            internal readonly VKeys Vkcode;
            internal readonly int ScanCode;
            internal readonly int Flags;
            internal readonly int Time; // Милисикунды между сообщениями. Обнуляются при переполнинии.
            internal readonly UIntPtr DwExtraInfo;   //??
        }
        internal struct KeyStats
        {
            internal bool Extendedkey;
            internal bool EventjectedisLow;
            internal bool EventIsInjected;
            internal bool ALTkeyIsPpressed;
            internal bool KeyNotIsPressed;
            internal int CountRepeat;

            public static implicit operator KeyStats(int flags) => new KeyStats
            {
                Extendedkey = Convert.ToBoolean(flags >> 0),
                EventjectedisLow = Convert.ToBoolean(flags >> 1),
                EventIsInjected = Convert.ToBoolean(flags >> 4),
                ALTkeyIsPpressed = Convert.ToBoolean(flags >> 5),
                KeyNotIsPressed = Convert.ToBoolean(flags >> 7),
                CountRepeat = Convert.ToInt32(flags >> 15),
            };

        }

        internal class SettingHook
        {
            internal bool Break { get; set; } = default;
        }

        #region WinAPI
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookHandler lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        #endregion
    }
}

