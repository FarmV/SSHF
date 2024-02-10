using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace SSHF.Infrastructure.TrayIconManagment
{
    internal partial class DPIIconHandler : IDisposable
    {
        private const int SM_CYICON = 12; //высота
        private const int SM_CXICON = 11; //ширина
        private bool _disposed = false;
        public readonly int[] _sizeIcon = [16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 128, 256, 512];
        private readonly DpiHandler _dpiHendler;
        private readonly Stream _iconAppResource;
        public event EventHandler<Icon>? ActualSizeIcon;
        public DPIIconHandler(Stream resourceIcon, int[]? sizesIcon = null)
        {
            _sizeIcon = sizesIcon ?? _sizeIcon;
            _iconAppResource = resourceIcon;
            _dpiHendler = new DpiHandler();
            _dpiHendler.DPIChange += DpiHandler_DPIChange;
        }
        [LibraryImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static partial int GetSystemMetricsForDpi(int nIndex, uint dpi); // Просто пересчитывает те же самые, закэшированные метрики, только только с учётом DPI
        public Icon GetDefaultStartProccesIconDPI()
        {
            Icon returnIcon = new Icon(_iconAppResource);
            _iconAppResource.Position = 0;
            return returnIcon;
        }
        private static int FindClosestNumber(int[] array, int target)
        {
            int closestNumber = array[0];

            foreach (int number in array)
            {
                if (Math.Abs(number - target) < Math.Abs(closestNumber - target)) closestNumber = number;               
            }

            return closestNumber;
        }
        private void DpiHandler_DPIChange(object? sender, DpiScale e)
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
                _dpiHendler.DPIChange -= DpiHandler_DPIChange;
                _dpiHendler.Dispose();
                _disposed = true;
            }
        }
        ~DPIIconHandler() { Dispose(false); }
    }
}
