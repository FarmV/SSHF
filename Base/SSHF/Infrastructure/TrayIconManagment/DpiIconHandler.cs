using DynamicData.Experimental;

using Linearstar.Windows.RawInput;
using SSHF.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SSHF.Infrastructure.TrayIconManagment
{
    internal class DpiIconHandler : IDisposable
    {
        private readonly Stream _iconAppResource;
        private DpiHandler _dpiHendler;
        private bool _disposed = false;

        private const int SM_CYICON = 12; //высота
        private const int SM_CXICON = 11; //ширина

        public readonly int[] _sizeIcon = new int[] { 16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 128, 256, 512 };

        public event EventHandler<Icon>? ActualSizeIcon;

        [DllImport("user32", SetLastError = true)]
        private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi); // Просто пересчитывает те же самые, закэшированные метрики, только только с учётом DPI
        public DpiIconHandler(Stream resourceIcon, int[]? sizesIcon = null)
        {
            _sizeIcon = sizesIcon ?? _sizeIcon;
            _iconAppResource = resourceIcon;
            _dpiHendler = new DpiHandler();
            _dpiHendler.DPIChange += _dpiHandler_DPIChange;
        }
        public Icon GetDefaultStartProccesIconDPI()
        {
            Icon returnIcon = new Icon(_iconAppResource);
            _iconAppResource.Position = 0;
            return returnIcon;
        }

        private int FindClosestNumber(int[] array, int target)
        {
            int closestNumber = array[0];

            foreach (int number in array)
            {
                if (Math.Abs(number - target) < Math.Abs(closestNumber - target))
                {
                    closestNumber = number;
                }
            }

            return closestNumber;
        }
        private void _dpiHandler_DPIChange(object? sender, DpiScale e)
        {
            int heightSystemICONDPIY = GetSystemMetricsForDpi(SM_CYICON, (uint)e.DpiScaleY);
            int widthSystemICO2NDPIX = GetSystemMetricsForDpi(SM_CXICON, (uint)e.DpiScaleX);
            if (heightSystemICONDPIY is 0 || widthSystemICO2NDPIX is 0)
            {
                throw new InvalidOperationException(Marshal.GetLastPInvokeErrorMessage());
            }

            System.Drawing.Size newSizeIcon = default;

            if (_sizeIcon.Contains(heightSystemICONDPIY) is true)
            {
                newSizeIcon.Width = heightSystemICONDPIY;
                newSizeIcon.Height = widthSystemICO2NDPIX;
            }
            else
            {
                int result = FindClosestNumber(_sizeIcon, heightSystemICONDPIY);
                newSizeIcon.Width = result;
                newSizeIcon.Height = result;
            }

            Icon returnIcon = new Icon(_iconAppResource, newSizeIcon);
            _iconAppResource.Position = 0;

            ActualSizeIcon?.Invoke(this, returnIcon);
        }

        public void Dispose()
        {
            if (_disposed is true) return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed is not true)
            {
                _dpiHendler.DPIChange -= _dpiHandler_DPIChange;
                _dpiHendler.Dispose();
                _disposed = true;
            }
        }
        ~DpiIconHandler() { Dispose(false); }
    }

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed is not true)
            {
                Dispatcher.FromThread(threadHendlerDPI)?.Invoke(() => { Dispatcher.FromThread(threadHendlerDPI)?.InvokeShutdown(); });
                _disposed = true;
            }
        }
        ~DpiHandler() { Dispose(false); }
    }
}
