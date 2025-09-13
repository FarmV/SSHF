using System;
using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FVH.SSHF.Infrastructure
{

    public sealed partial class WPFDropImageFile : IDisposable
    {
        internal bool IsDisposed = false;
        private string _lastFileName = string.Empty;
        private readonly Window _window;
        private readonly VirtualFileDragDrop _dragDropHandler;
        private BitmapSource? _lastDropImage;
        private MemoryStream? _lastImageStream;

        public WPFDropImageFile(Window window)
        {
            _window = window;
            _dragDropHandler = new VirtualFileDragDrop();
        }
        public void Dispose()
        {
            if(IsDisposed) return;
            IsDisposed = true;
            _lastImageStream?.Dispose();
        }
   
        private static nint _hookHandle = nint.Zero;
        public void SaveImageFromDrop(object ev, BitmapSource image)
        {
            if(ev is not MouseEventArgs) return;

            
            _ = Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution);
            Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread();


            if(CompareBitmapSources(_lastDropImage, image) is false)
            {
                _lastDropImage = image;
                _lastImageStream?.Dispose();
                _lastImageStream = new MemoryStream();

                PngBitmapEncoder encoder = new PngBitmapEncoder();

                BitmapFrame frame = BitmapFrame.Create(image, null, null, null);
                encoder.Frames.Add(frame);

                encoder.Save(_lastImageStream);
                _lastFileName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.png";
            }

            if(_lastImageStream is null || _lastDropImage is null) return;

            _lastImageStream.Position = 0;

            _dragDropHandler.InitiateDrop(_lastImageStream, _lastFileName, new VirtualFileDragDrop.DragDropOptions
            {
                CursorOffset = new VirtualFileDragDrop.POINT
                {
                    x = image.PixelWidth / 2,
                    y = image.PixelHeight / 2
                },
                HBitmap = VirtualFileDragDrop.Helper.CreateHBitmapFromBitmapSource(image)
            });
        }
        private static unsafe bool CompareBitmapSources(BitmapSource? bitmapSource1, BitmapSource? bitmapSource2)
        {
            if(bitmapSource1 is null || bitmapSource2 is null) return false;
            if(ReferenceEquals(bitmapSource1, bitmapSource2)) return true;

            if(bitmapSource1.PixelWidth != bitmapSource2.PixelWidth ||
                bitmapSource1.PixelHeight != bitmapSource2.PixelHeight ||
                bitmapSource1.Format != bitmapSource2.Format)
            {
                return false;
            }

            int bytesPerPixel = (bitmapSource1.Format.BitsPerPixel + 7) / 8;
            int stride = bitmapSource1.PixelWidth * bytesPerPixel;
            int size = bitmapSource1.PixelHeight * stride;

            byte[]? array1 = null;
            byte[]? array2 = null;

            try
            {
                Span<byte> pixels1Buffer = size <= 1024 ? stackalloc byte[size] : (array1 = ArrayPool<byte>.Shared.Rent(size));
                Span<byte> pixels2Buffer = size <= 1024 ? stackalloc byte[size] : (array2 = ArrayPool<byte>.Shared.Rent(size));

                pixels1Buffer = pixels1Buffer.Slice(0, size);
                pixels2Buffer = pixels2Buffer.Slice(0, size);

                fixed(byte* pPixels1 = pixels1Buffer, pPixels2 = pixels2Buffer)
                {
                    bitmapSource1.CopyPixels(new Int32Rect(0, 0, bitmapSource1.PixelWidth, bitmapSource1.PixelHeight), (nint)pPixels1, size, stride);

                    bitmapSource2.CopyPixels(new Int32Rect(0, 0, bitmapSource2.PixelWidth, bitmapSource2.PixelHeight), (nint)pPixels2, size, stride);
                }

                return pixels1Buffer.SequenceEqual(pixels2Buffer);
            }
            finally
            {
                if(array1 is not null) ArrayPool<byte>.Shared.Return(array1);
                if(array2 is not null) ArrayPool<byte>.Shared.Return(array2);
            }
        }   
    }
}