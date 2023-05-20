using SSHF.Infrastructure.SharedFunctions;

using System;
using System.Buffers.Binary;
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
                SaveBitmapSourceFromDrop(ev, bitSource);
            }
            finally
            {
                if (File.Exists(fileTmpPath) is true) File.Delete(fileTmpPath);
            }

        }
    }
}
internal class ImageManager : IGetImage
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
                IDataObject data = Clipboard.GetDataObject();
                var bba = data.GetFormats();

                if (data.GetDataPresent("PNG") is not true)
                {
                    if (data.GetDataPresent(DataFormats.Dib) is true)
                    {
                        using MemoryStream stream = (MemoryStream)data.GetData(DataFormats.Dib);
                        MemoryStream imagePngStram = ClipboardDibConverter.ConvertToPng(stream);
                        MemoryStream copyOriginalImage = new MemoryStream(imagePngStram.ToArray());

                        BitmapImage ImageInfo = new BitmapImage();
                        ImageInfo.BeginInit();
                        ImageInfo.StreamSource = imagePngStram;
                        ImageInfo.CacheOption = BitmapCacheOption.OnLoad;
                        ImageInfo.EndInit();

                        BitmapImage returnImageDPI96 = new BitmapImage();
                        returnImageDPI96.BeginInit();
                        returnImageDPI96.StreamSource = copyOriginalImage;
                        returnImageDPI96.DecodePixelWidth = (int)(ImageInfo.PixelWidth * ImageInfo.DpiX / 96d);
                        returnImageDPI96.DecodePixelHeight = (int)(ImageInfo.PixelHeight * ImageInfo.DpiY / 96d);
                        returnImageDPI96.CacheOption = BitmapCacheOption.OnLoad;
                        returnImageDPI96.EndInit();

                        returnImage = returnImageDPI96;
                        RenderOptions.SetBitmapScalingMode(ImageInfo, BitmapScalingMode.NearestNeighbor);                        
                        returnImage.Freeze();
                    }
                }
                else
                {
                    BitmapSource res = Clipboard.GetImage();
                    RenderOptions.SetBitmapScalingMode(res, BitmapScalingMode.NearestNeighbor);
                    res.Freeze();
                    returnImage = res;
                }
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

    private static class ClipboardDibConverter
    {
        public const string Dib = "DeviceIndependentBitmap";
        internal static MemoryStream ConvertToPng(MemoryStream ms)
        {
            if (ms == null) throw new ArgumentNullException(nameof(ms));
            ReadOnlySpan<byte> bytes = ms.ToArray();

            int headerSize = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            // Only supporting 40-byte DIB from clipboard
            if (headerSize != 40) throw new ArgumentException("Unsupported DIB header size");

            ReadOnlySpan<byte> header = bytes[..headerSize];
            int dataOffset = headerSize;
            int width = BinaryPrimitives.ReadInt32LittleEndian(header[4..]);
            int height = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
            short planes = BinaryPrimitives.ReadInt16LittleEndian(header[12..]);
            short bitCount = BinaryPrimitives.ReadInt16LittleEndian(header[14..]);

            //Compression: 0 = RGB; 3 = BITFIELDS.
            int compression = BinaryPrimitives.ReadInt32LittleEndian(header[16..]);

            // Not dealing with non-standard formats.
            if (planes != 1 || (compression != 0 && compression != 3)) throw new ArgumentException("Unsupported DIB compression type");

            System.Drawing.Imaging.PixelFormat fmt = bitCount switch
            {
                32 => System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                24 => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                16 => System.Drawing.Imaging.PixelFormat.Format16bppRgb555,
                _ => throw new ArgumentException("Unsupported DIB pixel format")
            };

            if (compression == 3) dataOffset += 12;
            if (bytes.Length < dataOffset) throw new ArgumentException("Wrong DIB image data length");

            byte[] image = bytes[dataOffset..].ToArray();
            if (compression == 3)
            {
                uint redMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[headerSize..]);
                uint greenMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(headerSize + 4)..]);
                uint blueMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(headerSize + 8)..]);

                if (bitCount == 32 && redMask == 0xFF0000 && greenMask == 0x00FF00 && blueMask == 0x0000FF)
                {
                    for (int pix = 3; pix < image.Length; pix += 4)
                    {
                        if (image[pix] != 0)
                        {
                            fmt = System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
                            break;
                        }
                    }
                }
                else throw new ArgumentException("Unsupported DIB pixel bitmask format");
            }
            using Bitmap bmp = CreateBitmap(image, width, height, fmt);
            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            MemoryStream result = new MemoryStream();
            bmp.Save(result, ImageFormat.Png);
            return result;
        }

        private static Bitmap CreateBitmap(byte[] bytes, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            Bitmap bmp = new Bitmap(width, height, pixelFormat);
            BitmapData bmpData = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(bytes, 0, bmpData.Scan0, height * bmpData.Stride);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

    }
}

