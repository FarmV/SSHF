using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SSHF.Infrastructure
{
    public class WPFDropImageFile : IDisposable
    {
        private readonly Window _window;
        private string _fileTmpPath = string.Empty;
        private DataObject _dropData;
        private Timer? _clearTmpTimer;
        private BitmapSource? _previousImage;
        public WPFDropImageFile(Window window)
        {
            _window = window;
            _dropData = new DataObject();
        }
        public void Dispose()
        {
            ClearTmpFile();
            _clearTmpTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
        public void SaveImageFromDrop(object ev, ImageSource image)
        {
            if (image is not BitmapSource bitSource) throw new InvalidCastException();
            SaveBitmapSourceFromDrop(ev, bitSource);
        }
        private void CreateTMPFile(string name)
        {
            ClearTmpFile();
            _fileTmpPath = Path.ChangeExtension($"{Path.GetTempPath()}{name}", "png");
            File.Create(_fileTmpPath).Dispose();
            File.SetAttributes(_fileTmpPath, FileAttributes.Temporary);
            _clearTmpTimer = new Timer(new TimerCallback((_) =>
            {
                ClearTmpFile();
            }), null, 300_000, Timeout.Infinite);
            _clearTmpTimer.Dispose();
        }
        private void ClearTmpFile()
        {
            if (_fileTmpPath is null) return;
            if (File.Exists(_fileTmpPath) is true) File.Delete(_fileTmpPath);
        }
        private void SetDataObject()
        {
            string[] arrayDrops = new string[] { _fileTmpPath };
            DataObject dataObject = new DataObject(DataFormats.FileDrop, arrayDrops);
            dataObject.SetData(DataFormats.StringFormat, dataObject);
            _dropData = dataObject;
        }
        private void SaveBitmapSourceFromDrop(object ev, BitmapSource image)
        {
            if (ev is not MouseEventArgs) return;

            if (CompareBitmapSources(_previousImage, image) is false)
            {
                _previousImage = image;

                string random = Path.GetRandomFileName().ToUpper();

                CreateTMPFile($"{random.Remove(random.Length - 4)}.png");
                using FileStream createFile = new FileStream(_fileTmpPath, FileMode.Truncate);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(createFile);
                SetDataObject();
            }
            else
            {
                DragDrop.DoDragDrop(_window, _dropData, DragDropEffects.Copy);
            }
        }
        private bool CompareBitmapSources(BitmapSource? bitmapSource1, BitmapSource? bitmapSource2)
        {
            if (bitmapSource1 is not BitmapSource || bitmapSource2 is not BitmapSource) return false;
            if (bitmapSource1.PixelWidth != bitmapSource2.PixelWidth || bitmapSource1.PixelHeight != bitmapSource2.PixelHeight) return false;

            PixelFormat pixelFormat1 = bitmapSource1.Format;
            PixelFormat pixelFormat2 = bitmapSource2.Format;

            if (pixelFormat1 != pixelFormat2) return false;

            int bytesPerPixel = (pixelFormat1.BitsPerPixel + 7) / 8;

            int stride = bitmapSource1.PixelWidth * bytesPerPixel;
            int size = bitmapSource1.PixelHeight * stride;

            byte[] pixels1 = new byte[size];
            byte[] pixels2 = new byte[size];

            bitmapSource1.CopyPixels(pixels1, stride, 0);

            bitmapSource2.CopyPixels(pixels2, stride, 0);

            for (int i = 0; i < size; i++)
            {
                if (pixels1[i] != pixels2[i]) return false;
            }

            return true;
        }
    }
}

