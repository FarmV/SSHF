using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FVH.SSHF.Infrastructure
{
    internal static class MsScreenClip
    {
        internal const string UriScheme = "ms-screenclip:";
        internal const string ProcessName = "ScreenClippingHost";
        internal static void Invoke()
        {
            if(IsEnableProcessHost() is true) return;
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "ms-screenclip:",
                UseShellExecute = true
            };
            _ = System.Diagnostics.Process.Start(processStartInfo);
        }
        internal static bool IsEnableProcessHost()
        {
            Process[] msScreenClipProc = System.Diagnostics.Process.GetProcessesByName(ProcessName);
            if(msScreenClipProc.Length is 0 ) return false;
            if(msScreenClipProc.Length > 1) Throw(); [DoesNotReturn] static void Throw() => throw new ArgumentOutOfRangeException(nameof(msScreenClipProc));
            if
            (
             msScreenClipProc.Length is 1 &&
             msScreenClipProc[0].HasExited is true
            ) return false;
            else return true;                    
        }
    }
}