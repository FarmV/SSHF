using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FVH.Background.Input
{
    internal partial class LowLevelKeyHook
    {
        [DllImport("user32")]
        private static extern nint SetWindowsHookEx(int idHook, KeyboardHookHandler lpfn, nint hMod, uint dwThreadId);

        [LibraryImport("user32")]
        [return : MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32")]
        private static extern nint CallNextHookEx(nint hhk, int nCode, WMEvent wParam, TagKBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern nint GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
    }
}
