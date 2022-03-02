using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using SSHF.Infrastructure.Interfaces;

namespace SSHF.Infrastructure.ImplementingInterfaces.ImageOperations
{
    internal class ImageOperations: IImageOperations
    {
        public event Action<object?>? Сompleted;

        public Task<Tuple<bool,object?,string>> ConvetImage(IImageOperations.ImageType imageInputType, IImageOperations.ImageType imageOutputType, object image)
        {

            IImageOperations.ImageType query = imageInputType & imageOutputType;

            switch (query)

            {   //BitmapSource to BitmapImage
                case IImageOperations.ImageType.SystemWindowsMediaImagingBitmapSource & IImageOperations.ImageType.SystemWindowsMediaImagingBitmapImage:
                    {
                        if (ConvertBitmapSourceToBitmapImage(image as BitmapSource) is not BitmapImage resultCovertation) return Task.FromResult(Tuple.Create<bool,object?,string>(false, null, "Несоотвесвие выбранных типов"));

                        return Task.FromResult(Tuple.Create<bool, object?, string>(true, resultCovertation, "Несоотвесвие выбранных типов"));
                    }

                default: return Task.FromResult(Tuple.Create<bool, object?, string>(false, null, "Конвертационная пара не поддерживается"));
            }
        }

        public bool ScaleImage(IImageOperations.ImageType imageInput, IImageOperations.ImageType imageOutput, double sacle)
        {
            throw new NotImplementedException();
        }



        System.Windows.Media.Imaging.BitmapImage ConvertBitmapImagToBitmapSource(System.Windows.Media.Imaging.BitmapImage source)
        {




            throw new NotImplementedException();
        }

        System.Windows.Media.Imaging.BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap source)
        {
            throw new NotImplementedException();
        }

        System.Drawing.Bitmap ConvertBitmapSourceToBitmapSource(System.Windows.Media.Imaging.BitmapSource source)
        {
            Bitmap bmp = new Bitmap(source.PixelWidth,source.PixelHeight,PixelFormat.Format32bppPArgb);

            BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size),ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

            source.CopyPixels(Int32Rect.Empty,data.Scan0,data.Height * data.Stride,data.Stride);

            bmp.UnlockBits(data);
            return bmp;
        }

        System.Drawing.Bitmap ConvertBitmapImageToBitmap(System.Windows.Media.Imaging.BitmapImage source)
        {
            throw new NotImplementedException();
        }

        System.Windows.Media.Imaging.BitmapImage? ConvertBitmapSourceToBitmapImage(System.Windows.Media.Imaging.BitmapSource? source)
        {
            if (source is not BitmapSource inputSource) return null;




            throw new NotImplementedException();
        }

        System.Windows.Media.Imaging.BitmapImage ConvertBitmapSourceToBitmapImage(System.Drawing.Bitmap source)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                source.Save(stream, ImageFormat.Png); 

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();

                result.CacheOption = BitmapCacheOption.OnLoad;

                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }




    }
}
