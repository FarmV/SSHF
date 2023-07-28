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
        private readonly Dispatcher _dispatcher;
        private string _fileTmpPath = string.Empty;
        private DataObject _dropData;
        private Timer? _clearTmpTimer;
        private BitmapSource _previousImage;
        public void Dispose()
        {
            ClearTmpFile();
            _clearTmpTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
        public WPFDropImageFile(Window window, Dispatcher dispatcher)
        {
            _window = window;
            _dispatcher = dispatcher;
            _dropData = new DataObject();
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
            if (_previousImage != image)
            {
                _previousImage = image;

                string random = Path.GetRandomFileName().ToUpper();

                CreateTMPFile($"{image.Height}x{image.Width}_{random.Remove(random.Length -8)}.png");
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
    }
}

