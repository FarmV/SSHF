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
        internal static void Invoke()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "ms-screenclip:",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
    }
}
