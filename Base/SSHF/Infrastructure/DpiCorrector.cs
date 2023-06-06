using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SSHF.Infrastructure
{
    public class DpiCorrector
    {
        private readonly Window _window;
        private readonly Dispatcher _dispatcher;
        public DpiCorrector(Window window, Dispatcher dispatcher)
        {
            _window = window;
            _dispatcher = dispatcher;
        }
        public DpiSacaleMonitor GetCurretDPI() => _dispatcher.Invoke(() =>
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(_window);
            return new DpiSacaleMonitor(dpi.DpiScaleX, dpi.DpiScaleY);
        });
    }
}

