using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;

namespace FVH.Background.Input
{
    public class LowLevlHook : CriticalFinalizerObject, IDisposable
    {
        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed is true) return;
            UninstallHook();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private delegate IntPtr KeyboardHookHandler(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam);
        private KeyboardHookHandler? hookHandler;

        private IntPtr hookID = IntPtr.Zero;

        public void InstallHook()
        {
            CheckDisposed();
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);
        }

        ~LowLevlHook()
        {
            try
            {
                if (_disposed is true) return;
                UnhookWindowsHookEx(hookID);
                Marshal.FreeHGlobal(hookID);
            }
            catch { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed() { if (_disposed is true) throw new ObjectDisposedException("You cannot use an instance of a class after it has been disposed of."); }


        public void UninstallHook() { CheckDisposed(); UnhookWindowsHookEx(hookID); }


        private const int WH_KEYBOARD_LL = 13;
        private const int WH_KEYBOARD = 2;
        private IntPtr SetHook(KeyboardHookHandler proc) =>
                        SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                         GetModuleHandleW(Process.GetCurrentProcess().MainModule is not ProcessModule module2 ?
                          throw new NullReferenceException() : module2.ModuleName ?? throw new NullReferenceException()), 0);



        internal delegate void KeyboardHookCallback(VKeys key, SettingHook setting);
        internal event KeyboardHookCallback? KeyDown;


        internal SettingHook Settings = new SettingHook();


      //  private List<VKeys> _keys = new List<VKeys>();
        private IntPtr HookFunc(int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam)
        {
            if (nCode is 0)
            {
                if(KeyDown is null) CallNextHookEx(hookID, nCode, wParam, lParam);

                //if (wParam is WMEvent.WM_KEYUP || wParam is WMEvent.WM_SYSKEYUP)
                //{
                //    if (_keys.Contains(lParam.Vkcode) is true)
                //    {
                //        _keys.Remove(lParam.Vkcode);
                //        CallNextHookEx(hookID, nCode, wParam, lParam);
                //    }
                //    else { CallNextHookEx(hookID, nCode, wParam, lParam); }
                //}
                if (wParam is WMEvent.WM_KEYDOWN || wParam is WMEvent.WM_SYSKEYDOWN)
                {
                    
                      //  _keys.Add(lParam.Vkcode);
                        KeyDown?.Invoke(lParam.Vkcode, Settings);
                        if (Settings.Break is true)
                        {
                            Settings.Break = false;
                            return (System.IntPtr)1;
                        }
                    

                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        enum WMEvent : uint
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
            internal readonly uint ScanCode;
            internal readonly uint Flags;
            internal readonly uint Time; // Милисикунды между сообщениями. Обнуляются при переполнинии.
            internal readonly UIntPtr DwExtraInfo;   //??
        }

        //internal struct KeyStats1
        //{
        //    internal bool Extendedkey;
        //    internal bool EventjectedisLow;
        //    internal bool EventIsInjected;
        //    internal bool ALTkeyIsPpressed;
        //    internal bool KeyNotIsPressed;
        //    internal bool LLKHF_UP;

        //    public static implicit operator KeyStats1(uint flags) => new KeyStats1
        //    {

        //        Extendedkey = Convert.ToBoolean(flags & 0b0),
        //        EventjectedisLow = Convert.ToBoolean(flags & 0b1),
        //        EventIsInjected = Convert.ToBoolean(flags & 0b100),
        //        ALTkeyIsPpressed = Convert.ToBoolean(flags & 0b101),
        //        KeyNotIsPressed = Convert.ToBoolean(flags & 7),
        //        LLKHF_UP = Test(flags)
        //    };

        //    public static bool Test(uint flags)
        //    {
        

        //        var result = Convert.ToBoolean(flags & 7);
        //        if (result is true) return true;
        //        else return false;

        //        Console.WriteLine();
        //        throw new NotImplementedException();
        //    }

        //}
        internal struct KeyStats
        {
            internal ushort RrepeatCount;
            internal byte ScanCode;
            internal bool Extendedkey;
            internal bool ContextCodeAlt;
            internal bool PreviousKeyStateOneIsPressKey; // 1 клавиша была нажата ранее, 0 если нет 
            internal bool TransitionState;// 0 при нажатии клавиши, 1 при отпускании

            public static implicit operator KeyStats(uint flags) => new KeyStats
            {
                RrepeatCount = (ushort)((0xFFFF >> 1) & flags),
                ScanCode = (byte)(8323072 & flags),
                Extendedkey = Convert.ToBoolean(flags & 24),
                ContextCodeAlt = Convert.ToBoolean(flags & 29),
                PreviousKeyStateOneIsPressKey = Convert.ToBoolean(flags & 30),
                TransitionState = Convert.ToBoolean(flags & 31)


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

