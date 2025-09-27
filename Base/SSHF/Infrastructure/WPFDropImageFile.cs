using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32.SafeHandles;

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
        public void SaveImageFromDrop(object ev, BitmapSource image)
        {
            if(ev is not MouseEventArgs mouseEventArgs) return; 
            
            _ = Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution);
            _ = Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread();

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

            (System.Windows.Size targetSize, double dpiScale) targetSize = GetDragImageTargetSize(_window,scaleFactor: 1.90);

            BitmapSource scaledDragImage = CreateScaledDragImage(_lastDropImage, targetSize.targetSize, targetSize.dpiScale);

            double scaleRatio = scaledDragImage.PixelWidth / (double)_lastDropImage.PixelWidth;

            System.Windows.Point clickPositionInDiu    = mouseEventArgs.GetPosition(_window);
            System.Windows.Point clickPositionInPixels = ConvertDiuToPixels(clickPositionInDiu, _window);

            VirtualFileDragDrop.POINT cursorOffset = new VirtualFileDragDrop.POINT
            {
                x = (int)(clickPositionInPixels.X * scaleRatio),
                y = (int)(clickPositionInPixels.Y * scaleRatio)
            };

            using SafeHBitmapHandle hBitmap = new SafeHBitmapHandle(VirtualFileDragDrop.Helper.CreateHBitmapFromBitmapSource(scaledDragImage));

            _dragDropHandler.InitiateDrop(_lastImageStream, _lastFileName, new VirtualFileDragDrop.DragDropOptions
            {
                CursorOffset = cursorOffset,
                HBitmap = hBitmap.DangerousGetHandle(),
            });

            _ = Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread();
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
        private static BitmapSource CreateScaledDragImage(BitmapSource originalImage, System.Windows.Size targetPixelSize, double dpiScale)
        {
            double originalWidth  = originalImage.PixelWidth;
            double originalHeight = originalImage.PixelHeight;

            int finalPixelWidth;
            int finalPixelHeight;
                  
            double maxWidth = targetPixelSize.Height * 2.0;
          
            if(originalHeight <= targetPixelSize.Height)
            {
                
                if(originalWidth > maxWidth)
                {
           
                    double widthRatio = maxWidth / originalWidth;
                    finalPixelWidth = (int)Math.Round(maxWidth);
                    finalPixelHeight = (int)Math.Round(originalHeight * widthRatio);
                }
                else
                {
                    
                    finalPixelWidth = (int)originalWidth;
                    finalPixelHeight = (int)originalHeight;
                }
            }
            else 
            {

                double heightRatio = targetPixelSize.Height / originalHeight;
                double potentialWidth = originalWidth * heightRatio;

                if(potentialWidth <= maxWidth)
                {
                    finalPixelWidth  = (int)Math.Round(potentialWidth);
                    finalPixelHeight = (int)Math.Round(targetPixelSize.Height);
                }
                else
                {
                    double widthRatio = maxWidth / originalWidth;
                    finalPixelWidth =  (int)Math.Round(maxWidth);
                    finalPixelHeight = (int)Math.Round(originalHeight * widthRatio);
                }
            }

            double targetDiuWidth  = finalPixelWidth / dpiScale;
            double targetDiuHeight = finalPixelHeight / dpiScale;

            DrawingVisual drawingVisual = new DrawingVisual();
            using(DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                Rect targetRect = new Rect(0, 0, targetDiuWidth, targetDiuHeight);
                drawingContext.DrawImage(originalImage, targetRect);
            }

            RenderTargetBitmap scaledBitmap = new RenderTargetBitmap(finalPixelWidth,finalPixelHeight,96, 96, PixelFormats.Pbgra32);

            scaledBitmap.Render(drawingVisual);
            scaledBitmap.Freeze();

            return scaledBitmap;
        }
        public static System.Windows.Point ConvertDiuToPixels(System.Windows.Point pointInDiu, Window windowContext)
        {
            PresentationSource? source = PresentationSource.FromVisual(windowContext);
            if(source is null) Throw(); [DoesNotReturn] static void Throw() => throw new InvalidOperationException("Cannot get PresentationSource from window.");

            Matrix matrix = source.CompositionTarget.TransformToDevice;

            return matrix.Transform(pointInDiu);
        }
        public static (System.Windows.Size targetPixelSize, double dpiScale) GetDragImageTargetSize(Window windowContext, double scaleFactor = 1.5)
        {
            PresentationSource? source = PresentationSource.FromVisual(windowContext);
            if(source is null) ThrowSource(); [DoesNotReturn] static void ThrowSource() => throw new InvalidOperationException("Cannot get PresentationSource from window.");
            Matrix matrix = source.CompositionTarget.TransformToDevice;
            double dpiScale = matrix.M11; 

            nint hwnd = new WindowInteropHelper(windowContext).Handle;
            if(hwnd is 0) ThrowHwnd(); [DoesNotReturn] static void ThrowHwnd() => throw new InvalidOperationException("Could not get window handle (HWND).");

            uint dpi = GetDpiForWindow(hwnd);
            if(dpi is 0) ThrowDpi(); [DoesNotReturn] static void ThrowDpi() => throw new InvalidOperationException("Could not get DPI for window.");

            const uint BASE_DPI = 96;
            int baseIconWidth  = GetSystemMetricsForDpi(SM_CXICON, BASE_DPI);
            int baseIconHeight = GetSystemMetricsForDpi(SM_CYICON, BASE_DPI);

            double targetWidth  = (baseIconWidth  * dpiScale) * scaleFactor;
            double targetHeight = (baseIconHeight * dpiScale) * scaleFactor;

            return (new System.Windows.Size(targetWidth, targetHeight), dpiScale);
        }
        private const int SM_CXICON = 11;
        private const int SM_CYICON = 12;

        [LibraryImport("user32")]
        private static partial uint GetDpiForWindow(nint hwnd);

        [LibraryImport("user32")]
        private static partial int GetSystemMetricsForDpi(int nIndex, uint dpi);

        internal sealed partial class SafeHBitmapHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeHBitmapHandle() : base(true) { }
            public SafeHBitmapHandle(nint hBitmap) : base(true)
            {
                SetHandle(hBitmap);
            }          
            protected override bool ReleaseHandle() =>  DeleteObject(handle);            
            [LibraryImport("gdi32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool DeleteObject(nint hObject);
        }
    }
}