using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SSHF
{
    public class DpiCorrector
    {
        private Window _window;
        private Dispatcher _dispatcher;
        public DpiCorrector(Window window, Dispatcher dispatcher)
        {
            _window = window;
            _dispatcher = dispatcher;
        }
        public DPISacaleMonitor GetCurretDPI() => _dispatcher.Invoke(() =>
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(_window);
            return new DPISacaleMonitor(dpi.DpiScaleX, dpi.DpiScaleY);
        });        
    }
}
public readonly struct DPISacaleMonitor
{
    public DPISacaleMonitor(double dpiScaleX, double dpiScaleY)
    {
        DpiScaleX = dpiScaleX;
        DpiScaleY = dpiScaleY;
    }
    public double DpiScaleX { get; }
    public double DpiScaleY { get; }
}

