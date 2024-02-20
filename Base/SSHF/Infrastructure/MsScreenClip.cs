using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                FileName = UriScheme,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
        internal static bool IsEnableProcessHost()
        {
            Process[] msScreenClipProc = System.Diagnostics.Process.GetProcessesByName(ProcessName);
            if(msScreenClipProc.Length is 0 ) return false;
            if(msScreenClipProc.Length > 1) throw new ArgumentOutOfRangeException(nameof(msScreenClipProc));
            if
            (
             msScreenClipProc.Length is 1 &&
             msScreenClipProc[0].HasExited is true
            ) return false;
            else return true;                    
        }
    }
}