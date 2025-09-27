using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FVH.SSHF.Infrastructure
{
    internal static class ImageFromClipboard
    {
        internal static async Task<BitmapSource?> GetClipboardImage(CancellationToken token = default)
        {
            const int MaxRetries       = 3;
            const int FirstRetryDelay  = 10; 
            const int SecondRetryDelay = 20;   
            const int FinalRetryDelay  = 32; 

            bool isCancelled = token.IsCancellationRequested;
            if(isCancelled) return default;
            for(int attempt = 0;attempt < MaxRetries;attempt++)
            {
                const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);
                try { return await GetClipboardImageInternal(token);}                
                catch(COMException comEx) when(comEx.HResult is CLIPBRD_E_CANT_OPEN)
                {
                    if(attempt is MaxRetries - 1) throw;
                    
                    int delay = attempt switch
                    {
                        0 => FirstRetryDelay,
                        1 => SecondRetryDelay,
                        _ => FinalRetryDelay
                    };

                    await Task.Delay(delay, token);
                }
                catch { throw; }
            }

            return default; 
        }
        private static Task<BitmapSource?> GetClipboardImageInternal(CancellationToken token = default)
        {
            bool isCancelled = token.IsCancellationRequested;
            if(isCancelled) throw new OperationCanceledException(token);

            BitmapSource? returnImage = null;
            ExceptionDispatchInfo? thrownException = null;

            Thread STAThread = new Thread(() =>
            {
                try
                {
                    if (token.IsCancellationRequested) return; 
                    
                    if (Clipboard.ContainsImage()) // Clipboard.GetImage поломан, он не только Dib(Старый Paint) возвращает пустым по пикселям, но и PNG скопированный из браузера
                    {
                        IDataObject data = Clipboard.GetDataObject();
                        const string PngType = "PNG";
                        const int DpiXDpiY   = 96;

                        if (!data.GetDataPresent(PngType))
                        {
                            if (data.GetDataPresent(DataFormats.Dib))
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
                                returnImageDPI96.DecodePixelHeight = Convert.ToInt32(ImageInfo.PixelHeight * ImageInfo.DpiY / DpiXDpiY);
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

                            BitmapDecoder image = PngBitmapDecoder.Create(imagePngStream, createOptions: BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                            BitmapSource frame = image.Frames[0];

                            if (image.Frames[0].DpiX is DpiXDpiY && image.Frames[0].DpiY is DpiXDpiY)
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
                }
                catch (Exception ex)
                {
                    thrownException = ExceptionDispatchInfo.Capture(ex);
                }
            });

            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            thrownException?.Throw();

            return Task.FromResult(returnImage);
        }
    }
}