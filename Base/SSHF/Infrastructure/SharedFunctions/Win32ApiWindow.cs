using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Microsoft.Win32.SafeHandles;

using SSHF.Views.Windows.Notify;

namespace SSHF.Infrastructure.SharedFunctions
{        
    internal class Win32ApiWindow
    {   
        internal class RefreshWindowPositin
        {           
            internal static async Task RefreshWindowPosCursor(Window window, CancellationToken token)
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
        
                while (token.IsCancellationRequested is not true)
                { 
                    await Task.Delay(1);   // узнать можно ли починитиь ошибку диспечера потоков окна?

                    Point point = CursorFunctions.GetCursorPosition();
                    await window.Dispatcher.BeginInvoke(() =>
                    {
                         SetWindowPos(helper.Handle, -1, Convert.ToInt32(point.X - 30), Convert.ToInt32(point.Y - 30),
                         Convert.ToInt32(window.Width), Convert.ToInt32(window.Height), 0x0400 | 0x0040);
                    }, System.Windows.Threading.DispatcherPriority.Send);
                }                
            }
            [DllImport("user32.dll")]
            internal static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);
        }   
    }
}
