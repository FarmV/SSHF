using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class WindowFunction
    {
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);

      
        private static readonly MainWindow _Window = new Func<MainWindow>(() => { if (App.Current.MainWindow is not MainWindow window) throw new NullReferenceException("MainWindow is null?"); return window;}).Invoke();
        
        public static MainWindow Window => _Window;



    }
}
