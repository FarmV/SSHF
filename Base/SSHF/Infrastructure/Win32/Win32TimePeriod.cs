using System.Runtime.InteropServices;

namespace SSHF.Infrastructure
{

    internal partial class Win32TimePeriod
    {
        internal const uint TIMERR_NOERROR = 0;
        internal const uint TIMERR_NOCANDO = 93;

        [LibraryImport("winmm", EntryPoint = "timeBeginPeriod")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static partial uint TimeBeginPeriod(uint uMilliseconds);
        [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static partial uint TimeEndPeriod(uint uMilliseconds);
    }

}
