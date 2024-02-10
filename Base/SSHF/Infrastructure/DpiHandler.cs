using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SSHF.Infrastructure.TrayIconManagment
{
    internal partial class DpiHandler : IDisposable
    {
        private const int WM_DPICHANGED = 0x02E0;
        private nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new nint(-4);
        private Thread? threadHendlerDPI;
        private volatile HwndSource? _proxyInputHandlerWindow = null;
        public event EventHandler<DpiScale>? DPIChange;
        private bool _disposed = false;

        public DpiHandler() => Init();
        [LibraryImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static partial int SetThreadDpiAwarenessContext(nint dpiContext);
        private void Init()
        {
            threadHendlerDPI = new Thread(() =>
            {
                if (SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero) // return 2147508241 вероятно возращаймое значение не корректно SetThreadDpiAwarenessContext должен возращать nint, из перечислеия DPI_AWARENESS_CONTEXT прошлого состояния потока
                {
                    string error = Marshal.GetLastPInvokeErrorMessage();
                    throw new InvalidOperationException(error);
                }

                HwndSourceParameters hwndSourceParameters = new HwndSourceParameters($"DpiHandler-{Path.GetRandomFileName}", 0, 0)
                {
                    WindowStyle = 0x800000
                };
                _proxyInputHandlerWindow = new HwndSource(hwndSourceParameters);
                Dispatcher.Run();
            })
            { Name = "Dpi Handler" };
            threadHendlerDPI.SetApartmentState(ApartmentState.STA);
            threadHendlerDPI.Start();


            if (SpinWait.SpinUntil(() => Dispatcher.FromThread(threadHendlerDPI) is not null &&
            _proxyInputHandlerWindow is not null, TimeSpan.FromMilliseconds(300)) is false) throw new InvalidOperationException("Dispatcher is null or _proxyInputHandlerWindow null");

            Dispatcher dispatcherDPIWindowHendler = Dispatcher.FromThread(threadHendlerDPI);

            dispatcherDPIWindowHendler.Invoke(() =>
            {
                _proxyInputHandlerWindow?.AddHook(WndProc);
            });

            nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
            {
                if (msg is WM_DPICHANGED)
                {
                    int dpiX = wParam.ToInt32() & 0xFFFF;
                    int dpiY = wParam.ToInt32() >> 16;

                    DPIChange?.Invoke(this, new DpiScale(dpiX, dpiY));

                    handled = true;
                }
                return hwnd;
            }
        }
        public void Dispose()
        {
            if (_disposed is true) return;
            Dispatcher.FromThread(threadHendlerDPI)?.Invoke(() => { Dispatcher.FromThread(threadHendlerDPI)?.InvokeShutdown(); });
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        ~DpiHandler() { Dispose(); }
    }
}
