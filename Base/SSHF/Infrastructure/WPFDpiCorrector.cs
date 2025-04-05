using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using ControlzEx.Standard;

namespace FVH.SSHF.Infrastructure
{
    public class WPFDpiCorrector
    {
        private const int  WM_GETDPISCALEDSIZE  = 0x02E4;
        private const int WM_DPICHANGED = 0x02E0;
        private readonly Window _window;
        private readonly Dispatcher _dispatcher;
        internal R3.BehaviorSubject<DpiScale> ChangeDpiCurrentWindow;
        public WPFDpiCorrector(Window window, Dispatcher dispatcher)
        {
            _window = window;
            _dispatcher = dispatcher;

            ChangeDpiCurrentWindow = new R3.BehaviorSubject<DpiScale>(default);

            _window.DpiChanged += WindowDpiChangedEvent;
        }
        private void WindowDpiChangedEvent(object sender, DpiChangedEventArgs e) => ChangeDpiCurrentWindow.OnNext(e.NewDpi);
        public DpiScale GetCurrentDPI() => _dispatcher.Invoke(() => _ = VisualTreeHelper.GetDpi(_window));         
    }
}