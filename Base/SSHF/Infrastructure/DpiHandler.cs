using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FVH.SSHF.Infrastructure.TrayIconManagement
{
    internal partial class DpiHandler : IDisposable
    {
        /// <summary>
        /// Примечание: WS_POPUP в Windows API обычно представлен как 32-битное значение (Int32),
        /// но для ясности и соответствия документации используетcя тип long (Int64).
        /// При приведении значения к типу int происходит потеря старших битов, но это безопасно,
        /// так как младшие 32 бита достаточны для представления стиля окна WS_POPUP.
        /// </summary>
        private const long WS_POPUP = 0x80000000L;
        private const int WM_DPICHANGED = 0x02E0;
        private Thread? _threadHandlerDPI;
        private volatile HwndSource? _proxyInputHandlerWindow = null;
        public event EventHandler<DpiScale>? DPIChange;
        private bool _disposed = false;

        public DpiHandler() => Init();

        private void Init()
        {
            _threadHandlerDPI = new Thread(() =>
            {
                if (App.Native.SetThreadDpiAwarenessContext(App.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) == nint.Zero)
                {
                    string error = Marshal.GetLastPInvokeErrorMessage();
                    throw new InvalidOperationException(error);
                }

                HwndSourceParameters hwndSourceParameters = new HwndSourceParameters($"DpiHandler-{Path.GetRandomFileName}", 0, 0)
                {
                    WindowStyle =unchecked ((int)WS_POPUP)
                };
                _proxyInputHandlerWindow = new HwndSource(hwndSourceParameters);
                Dispatcher.Run();
            })
            { Name = "Dpi Handler" };
            _threadHandlerDPI.SetApartmentState(ApartmentState.STA);
            _threadHandlerDPI.Start();

            if (SpinWait.SpinUntil(() => Dispatcher.FromThread(_threadHandlerDPI) is not null &&
            _proxyInputHandlerWindow is not null, TimeSpan.FromMilliseconds(300)) is false) throw new InvalidOperationException("Dispatcher is null or _proxyInputHandlerWindow null");

            Dispatcher dispatcherDPIWindowHandler = Dispatcher.FromThread(_threadHandlerDPI);

            dispatcherDPIWindowHandler.Invoke(() =>_proxyInputHandlerWindow?.AddHook(WndProc));

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
            Dispatcher.FromThread(_threadHandlerDPI)?.Invoke(() => { Dispatcher.FromThread(_threadHandlerDPI)?.InvokeShutdown(); });
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        ~DpiHandler() { Dispose(); }
    }
}