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

         

    internal class WindowFunctions
    {

     

        internal class RefreshWindowPositin
        {


            

            internal static async Task RefreshWindowPosCursor(Window window, CancellationToken token)
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
        
                while (token.IsCancellationRequested is not true)
                { 
                    await Task.Delay(1);   // узнать можно ли починитиь
                    CursorFunctions.POINT point;
                    CursorFunctions.GetCursorPos(out point);
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




        internal class RefreshStatusWinow
        {

            // To get a handle to the specified monitor
            [DllImport("user32.dll")]
            private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

            // To get the working area of the specified monitor
            [DllImport("user32.dll")]
            private static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MonitorInfoEx monitorInfo);

            private static MonitorInfoEx GetMonitorInfo(Window window, IntPtr monitorPtr)
            {
                var monitorInfo = new MonitorInfoEx();

                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                GetMonitorInfo(new HandleRef(window, monitorPtr), monitorInfo);

                return monitorInfo;
            }

            internal static void Minimize(Window window)
            {
                if (window == null)
                {
                    return;
                }

                window.WindowState = WindowState.Minimized;
            }

            internal static void Restore(Window window)
            {
                if (window == null)
                {
                    return;
                }

                window.WindowState = WindowState.Normal;
                window.ResizeMode = ResizeMode.CanResizeWithGrip;
            }

            internal static void Maximize(Window window)
            {
                window.ResizeMode = ResizeMode.NoResize;

                // Get handle for nearest monitor to this window
                var wih = new WindowInteropHelper(window);

                // Nearest monitor to window
                const int MONITOR_DEFAULTTONEAREST = 2;
                var hMonitor = MonitorFromWindow(wih.Handle, MONITOR_DEFAULTTONEAREST);

                // Get monitor info
                var monitorInfo = GetMonitorInfo(window, hMonitor);

                // Create working area dimensions, converted to DPI-independent values
                var source = HwndSource.FromHwnd(wih.Handle);

                if (source?.CompositionTarget == null)
                {
                    return;
                }

                var matrix = source.CompositionTarget.TransformFromDevice;
                var workingArea = monitorInfo.rcWork;

                var dpiIndependentSize =
                    matrix.Transform(
                        new Point(workingArea.Right - workingArea.Left,
                                  workingArea.Bottom - workingArea.Top));



                // Maximize the window to the device-independent working area ie
                // the area without the taskbar.
                window.Top = workingArea.Top;
                window.Left = workingArea.Left;

                window.MaxWidth = dpiIndependentSize.X;
                window.MaxHeight = dpiIndependentSize.Y;

                window.WindowState = WindowState.Maximized;


            }


            // Rectangle (used by MONITORINFOEX)
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            // Monitor information (used by GetMonitorInfo())
            [StructLayout(LayoutKind.Sequential)]
            public class MonitorInfoEx
            {
                public int cbSize;
                public Rect rcMonitor; // Total area
                public Rect rcWork; // Working area
                public int dwFlags;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
                public char[] szDevice;
            }

        }


    }
}
