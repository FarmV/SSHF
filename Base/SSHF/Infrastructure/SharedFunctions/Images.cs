using SSHF.Infrastructure.SharedFunctions;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


using Color = System.Drawing.Color;

namespace SSHF.Infrastructure.SharedFunctions
{
    public interface IGetImage
    {
        Task<ImageSource?> GetImageFromClipboard();
        Task<ImageSource?> GetImageFromFile(Uri path);

    }
    public class SetImage
    {
        private Window _window;
        private Dispatcher _dispatcher;
        private string fileTmpPath;
        public SetImage(Window window, Dispatcher dispatcher)
        {
            _window = window;
            _dispatcher = dispatcher;
        }

        private void _window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {           
            throw new NotImplementedException();
        }

        private void SaveBitmapSourceFromDrop(object ev, BitmapSource image)
        {
            if (ev is not System.Windows.Input.MouseEventArgs e) return;
            fileTmpPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
            File.Create(fileTmpPath).Dispose();
            File.SetAttributes(fileTmpPath, FileAttributes.Temporary);

            using (FileStream createFile = new FileStream(fileTmpPath, FileMode.OpenOrCreate))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(createFile);
            }

            string[] arrayDrops = new string[] { fileTmpPath };
            DataObject dataObject = new DataObject(DataFormats.FileDrop, arrayDrops);
            dataObject.SetData(DataFormats.StringFormat, dataObject);
            // if (e.MouseDevice.LeftButton == MouseButtonState.Pressed & )

            DragDrop.DoDragDrop(_window, dataObject, DragDropEffects.Copy);

        }
        public void SaveImageFromDrop(object ev, ImageSource image)
        {
            try
            {
                if (image is not BitmapSource bitSource) throw new InvalidCastException();
                SaveBitmapSourceFromDrop(ev,bitSource);
            }
            finally
            {
                if (File.Exists(fileTmpPath) is true) File.Delete(fileTmpPath);
            }

        }
    }
}
internal partial class ImageManager : IGetImage
{

    #region Извлечение изображения из буфера обмена
    /// <returns>Возращает изображание, помещённое в память, из буфера обмена. Или пустую ссылку.</returns>
    internal static Task<BitmapSource?> GetClipboardImage(CancellationToken token = default)
    {
        var isCancelled = token.IsCancellationRequested;
        if (isCancelled is true) throw new OperationCanceledException(token);

        BitmapSource? returnImage = null;
        Thread STAThread = new Thread(() =>
        {
            if (Clipboard.ContainsImage() is true)
            {
                BitmapSource res = Clipboard.GetImage();
                RenderOptions.SetBitmapScalingMode(res, BitmapScalingMode.NearestNeighbor);
                res.Freeze();
                returnImage = res;
            }

        });
        if (isCancelled is true) throw new OperationCanceledException(token);

        STAThread.SetApartmentState(ApartmentState.STA);
        STAThread.Start();
        STAThread.Join();

        return Task.FromResult(returnImage);
    }
    #endregion

    /// <summary>
    /// <br>Путь извлечения изображения <see cref="Uri"/> <paramref name="path"/></br>
    /// </summary>
    /// <returns>Возращает изображание, помещённое в память, или NULL в случае ошибки.</returns>
    internal static BitmapImage? GetBitmapImage(Uri path)
    {
        try
        {
            BitmapImage Image = new BitmapImage();
            Image.BeginInit();
            Image.UriSource = path;
            Image.CacheOption = BitmapCacheOption.OnLoad;  // Инициализаця файл был в памяти и не занят
            Image.EndInit();
            RenderOptions.SetBitmapScalingMode(Image, BitmapScalingMode.NearestNeighbor);
            return Image;
        }
        catch (Exception)
        {
            BitmapImage? Image = null;

            return Image;
        }
    }

    public async Task<ImageSource?> GetImageFromClipboard()
    {
        return await GetClipboardImage();
    }


    public Task<ImageSource?> GetImageFromFile(Uri path)
    {
        ImageSource? result = GetBitmapImage(path);
        return Task.FromResult(result);
    }
}

