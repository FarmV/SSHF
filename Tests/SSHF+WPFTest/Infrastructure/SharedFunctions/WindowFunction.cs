using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class WindowFunction
    {

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);
    
    

    }
}
