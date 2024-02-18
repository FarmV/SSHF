using System.Runtime.InteropServices;

using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal partial class LowLevelKeyHook
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct TagKBDLLHOOKSTRUCT
        {
            internal readonly VKeys VKCode;
            internal readonly uint ScanCode;
            internal readonly uint Flags;
            internal readonly uint Time; // GetMessageTime() для сообщениями. До переполнение примерно 49,71 дней.
            internal readonly UIntPtr DwExtraInfo; // ?
        }
    }
}

