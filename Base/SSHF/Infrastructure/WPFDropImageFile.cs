using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using MahApps.Metro.Controls;

using Microsoft.VisualBasic;

using Windows.Graphics.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

using WinRT;
using WinRT.Interop;

using static System.Windows.Forms.DataFormats;
using static FVH.SSHF.Infrastructure.VirtualFileDragDrop;

namespace FVH.SSHF.Infrastructure
{
    
    public sealed class WPFDropImageFile : IDisposable
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
            if(ev is not MouseEventArgs) return;

            if(CompareBitmapSources(_lastDropImage, image) is false)
            {
                _lastDropImage = image;

                _lastImageStream?.Dispose();
                _lastImageStream = new MemoryStream();
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                encoder.Save(_lastImageStream);

                string random = Path.GetRandomFileName().ToUpper();
                _lastFileName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.png";
            }

            if(_lastImageStream is null) return;

            _lastImageStream.Position = 0;

            _dragDropHandler.InitiateDrop(_lastImageStream, _lastFileName);
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