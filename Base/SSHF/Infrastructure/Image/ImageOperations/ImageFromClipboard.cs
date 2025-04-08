using System;
using System.Diagnostics;
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
                if (Clipboard.ContainsImage() is true) // Clipboard.GetImage поломан, он не только Dib(Старый Paint) возвращает пустым по пикселям, но и PNG скопированный из браузера
                {
                    IDataObject data = Clipboard.GetDataObject();
                    const string PngType = "PNG";
                    const int DpiXDpiY = 96;
                    if (data.GetDataPresent(PngType) is not true)
                    {
                        if (data.GetDataPresent(DataFormats.Dib) is true)
                        {
                            using MemoryStream stream = (MemoryStream)data.GetData(DataFormats.Dib);
                            MemoryStream imagePngStream = DibToBitmapConverter.ConvertToPng(stream);
                            MemoryStream copyOriginalImage = new MemoryStream(imagePngStream.ToArray());

                            BitmapImage ImageInfo = new BitmapImage();
                            ImageInfo.BeginInit();
                            ImageInfo.StreamSource = imagePngStream;
                            ImageInfo.CacheOption = BitmapCacheOption.OnLoad;
                            ImageInfo.EndInit();

                            BitmapImage returnImageDPI96 = new BitmapImage();
                            returnImageDPI96.BeginInit();
                            returnImageDPI96.StreamSource = copyOriginalImage;
                            returnImageDPI96.DecodePixelWidth = Convert.ToInt32(ImageInfo.PixelWidth * ImageInfo.DpiX / DpiXDpiY);
                            returnImageDPI96.DecodePixelHeight = Convert.ToInt32(ImageInfo.PixelHeight * ImageInfo.DpiY /DpiXDpiY);
                            returnImageDPI96.CacheOption = BitmapCacheOption.OnLoad;
                            returnImageDPI96.EndInit();

                            returnImage = returnImageDPI96;
                            RenderOptions.SetBitmapScalingMode(returnImageDPI96, BitmapScalingMode.NearestNeighbor);
                            returnImage.Freeze();
                        }
                    }
                    else
                    {
                        using MemoryStream imagePngStream = (MemoryStream)data.GetData(PngType);

                        BitmapDecoder image =  PngBitmapDecoder.Create(imagePngStream,createOptions: BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
                        BitmapSource frame = image.Frames[0];

                        if(image.Frames[0].DpiX is DpiXDpiY && image.Frames[0].DpiY is DpiXDpiY)
                        {
                            BitmapFrame modifiableFrame = BitmapFrame.Create(image.Frames[0]);
                            returnImage = modifiableFrame;
                            RenderOptions.SetBitmapScalingMode(returnImage, BitmapScalingMode.NearestNeighbor);
                            returnImage.Freeze();
                            return;
                        }

                        MemoryStream reEncodedStream = new MemoryStream();

                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image.Frames[0]));
                        encoder.Save(reEncodedStream);
                        reEncodedStream.Position = 0; 

                        BitmapImage scaledImage = new BitmapImage();
                        scaledImage.BeginInit();
                        scaledImage.CacheOption = BitmapCacheOption.OnLoad;
                        scaledImage.StreamSource = reEncodedStream;

                        scaledImage.DecodePixelWidth = Convert.ToInt32(image.Frames[0].PixelWidth * image.Frames[0].DpiX / DpiXDpiY);
                        scaledImage.DecodePixelHeight = Convert.ToInt32(image.Frames[0].PixelHeight * image.Frames[0].DpiY / DpiXDpiY);
                        scaledImage.EndInit();

                        returnImage = scaledImage;
                        RenderOptions.SetBitmapScalingMode(returnImage, BitmapScalingMode.NearestNeighbor);
                        returnImage.Freeze();
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