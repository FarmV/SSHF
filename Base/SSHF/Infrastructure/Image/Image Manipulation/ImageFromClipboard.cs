using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FVH.SSHF.Infrastructure
{
    internal static class ImageFromClipboard
    {
        internal static Task<BitmapSource?> GetClipboardImage(CancellationToken token = default)
        {
            bool isCancelled = token.IsCancellationRequested;
            if (isCancelled is true) throw new OperationCanceledException(token);

            BitmapSource? returnImage = null;
            Thread STAThread = new Thread(() =>
            {
                if (Clipboard.ContainsImage() is true)
                {
                    IDataObject data = Clipboard.GetDataObject();

                    if (data.GetDataPresent("PNG") is not true)
                    {
                        if (data.GetDataPresent(DataFormats.Dib) is true)
                        {
                            using MemoryStream stream = (MemoryStream)data.GetData(DataFormats.Dib);
                            MemoryStream imagePngStram = DibToBitmapConverter.ConvertToPng(stream);
                            MemoryStream copyOriginalImage = new MemoryStream(imagePngStram.ToArray());

                            BitmapImage ImageInfo = new BitmapImage();
                            ImageInfo.BeginInit();
                            ImageInfo.StreamSource = imagePngStram;
                            ImageInfo.CacheOption = BitmapCacheOption.OnLoad;
                            ImageInfo.EndInit();

                            BitmapImage returnImageDPI96 = new BitmapImage();
                            returnImageDPI96.BeginInit();
                            returnImageDPI96.StreamSource = copyOriginalImage;
                            returnImageDPI96.DecodePixelWidth = Convert.ToInt32(ImageInfo.PixelWidth * ImageInfo.DpiX / 96d);
                            returnImageDPI96.DecodePixelHeight = Convert.ToInt32(ImageInfo.PixelHeight * ImageInfo.DpiY / 96d);
                            returnImageDPI96.CacheOption = BitmapCacheOption.OnLoad;
                            returnImageDPI96.EndInit();

                            returnImage = returnImageDPI96;
                            RenderOptions.SetBitmapScalingMode(returnImageDPI96, BitmapScalingMode.NearestNeighbor);
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
    }
}

