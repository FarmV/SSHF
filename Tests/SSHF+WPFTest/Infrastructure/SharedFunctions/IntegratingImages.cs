using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Drawing.Color;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal static class IntegratingImages
    {
        #region Интеграция изображений

        #region Извлечение изображения из буфера обмена
        /// <returns>Возращает изображание, помещённое в память, из буфера обмена. Или пустую ссылку.</returns>
        internal static BitmapSource? GetBufferImage()
        {

            BitmapSource? image = Clipboard.GetImage();

            if (image is null) return image;

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            return image;
        }
        #endregion

        #region Изображения в память(инициализация)
        /// <summary>
        ///     <br>Путь извлечения изображения <see cref="Uri"/> <paramref name="path"/></br>
        /// </summary>
        /// <returns>Возращает изображание, помещённое в память, или вызывает исключение.</returns>
        internal static BitmapImage? SetImageToMemoryFromDrive(Uri path)
        {

            try
            {
                BitmapImage Image = new BitmapImage();              // Инициализаця файл был в памяти и не заянят
                Image.BeginInit();
                Image.UriSource = path;
                Image.CacheOption = BitmapCacheOption.OnLoad;
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

        internal static BitmapImage? ImageScale(BitmapImage ImageToScale)
        {

            try
            {

                BitmapImage? result1 = mySacle(ImageToScale,1.019);


                //TransformedBitmap? Transformed = new TransformedBitmap(ImageToScale, new ScaleTransform(1.5, 1.5)); // Ваш Sacle

                using MemoryStream outStream = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();

                enc.Frames.Add(BitmapFrame.Create(result1));
                enc.Save(outStream);

                using Bitmap bitmap = new Bitmap(outStream);

                bitmap.MakeTransparent();

                //  Bitmap? res2 = MakeTransparent(bitmap, System.Drawing.Color.FromArgb(255, 0, 0, 0), 765);
                // bitmap.MakeTransparent(System.Drawing.Color.FromArgb(255, 0, 0, 0));
                // res2.MakeTransparent(System.Drawing.Color.FromArgb(255, 0, 0, 0));
                // bitmap.MakeTransparent(bitmap.GetPixel(1, 1));


                // bitmap.MakeTransparent();

                //System.Drawing.Color backColor = bitmap.GetPixel(1, 1);

                // BitmapImage image = BitmapToBitmapImage(bitmap);

                //BitmapSource? res = CreateTransparency(Transformed);




                //BmpBitmapEncoder? encoder = new BmpBitmapEncoder();
                //using MemoryStream memoryStream = new MemoryStream();
                //BitmapImage bImg = new BitmapImage();




                //encoder.Frames.Add(BitmapFrame.Create(res));
                //encoder.Save(memoryStream);

                //memoryStream.Position = 0;
                //bImg.BeginInit();
                //bImg.StreamSource = memoryStream;
                //var ss = encoder.ColorContexts;
                //bImg.EndInit();


                // Bitmap output = res.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb)



                BitmapSource? res34 = GetScaledBitmap(ImageToScale,new ScaleTransform(1.010,1.010),true);



                using Bitmap bitmap32 = GetBitmap(res34);

               // var color32 = bitmap32.GetPixel(1, 1);

               // bitmap32.MakeTransparent(color32);


                var resFin = BitmapToBitmapImage(bitmap32);





                IntegratingImages.SafeImage(new Uri(@"C:\Users\Vikto\Pictures\test\TESTrecords.png"), resFin);
                return resFin;



            }
            catch (Exception)
            {
                BitmapImage? ImageNull = null;

                return ImageNull;
            }
        }

       static Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        internal static BitmapImage mySacle(BitmapImage ImageToScale, double scale)
        {
            //using MemoryStream outStream = new MemoryStream();

            //    BitmapEncoder enc = new BmpBitmapEncoder();
            //    enc.Frames.Add(BitmapFrame.Create(ImageToScale));
            //    enc.Save(outStream);
            //    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

            


            WriteableBitmap bmp = new WriteableBitmap(ImageToScale);
            // WriteableBitmap bmp = mipMap.BaseImage;
            int origWidth = bmp.PixelWidth;
            int origHeight = bmp.PixelHeight;
            int origStride = origWidth * 4;
            int newWidth = (int)(origWidth * scale);
            int newHeight = (int)(origHeight * scale);
            int newStride = newWidth * 4;



            // Pull out alpha since scaling with alpha doesn't work properly for some reason
            WriteableBitmap alpha = new WriteableBitmap(origWidth, origHeight, 96, 96, PixelFormats.Bgr32, null);
            unsafe
            {
                int index = 3;
                byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                byte* mainPtr = (byte*)bmp.BackBuffer.ToPointer();
                for (int i = 0;i < origWidth * origHeight * 3;i += 4)
                {
                    // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                    alphaPtr[i] = mainPtr[index];
                    alphaPtr[i + 1] = mainPtr[index];
                    alphaPtr[i + 2] = mainPtr[index];
                    alphaPtr[i + 3] = mainPtr[index];
                    index += 4;
                }
            }

            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);

            // Scale RGB and alpha
            ScaleTransform scaletransform = new ScaleTransform(scale, scale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);
            TransformedBitmap scaledAlpha = new TransformedBitmap(alpha, scaletransform);

            // Put alpha back in
            FormatConvertedBitmap newConv = new FormatConvertedBitmap(scaledMain, PixelFormats.Bgra32, null, 0);
            WriteableBitmap resized = new WriteableBitmap(newConv);
            WriteableBitmap newAlpha = new WriteableBitmap(scaledAlpha);
            unsafe
            {
                byte* resizedPtr = (byte*)resized.BackBuffer.ToPointer();
                byte* alphaPtr = (byte*)newAlpha.BackBuffer.ToPointer();
                for (int i = 3;i < newStride;i += 4)
                    resizedPtr[i] = alphaPtr[i];
            }




            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(newAlpha));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            
            return bmImage;





        }

        private static BitmapSource GetAphaAsGrayBitmap(BitmapSource rgba)
        {
            WriteableBitmap bmp = new WriteableBitmap(rgba);
            WriteableBitmap alpha = new WriteableBitmap(rgba.PixelWidth, rgba.PixelHeight, 96, 96, PixelFormats.Gray8, null);

            unsafe
            {
                byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                byte* mainPtr = (byte*)bmp.BackBuffer.ToPointer();
                for (int i = 0;i < bmp.PixelWidth * bmp.PixelHeight;i++)
                    alphaPtr[i] = mainPtr[i * 4 + 3];
            }

            return alpha;
        }

        private static BitmapSource MergeAlphaAndRGB(BitmapSource rgb, BitmapSource alpha)
        {
            // Put alpha back in
            WriteableBitmap dstW = new WriteableBitmap(new FormatConvertedBitmap(rgb, PixelFormats.Bgra32, null, 0));
            WriteableBitmap alphaW = new WriteableBitmap(alpha);
            unsafe
            {
                byte* resizedPtr = (byte*)dstW.BackBuffer.ToPointer();
                byte* alphaPtr = (byte*)alphaW.BackBuffer.ToPointer();
                for (int i = 0;i < dstW.PixelWidth * dstW.PixelHeight;i++)
                    resizedPtr[i * 4 + 3] = alphaPtr[i];
            }

            return dstW;
        }

        private static BitmapSource GetScaledBitmap(BitmapSource src, ScaleTransform scale, bool ReturnAlfa =false)
        {
            if (src.Format == PixelFormats.Bgra32) // special case when image has an alpha channel
            {
                // Put alpha in a gray bitmap and scale it
                BitmapSource alpha = GetAphaAsGrayBitmap(src);

               
                TransformedBitmap scaledAlpha = new TransformedBitmap(alpha, scale);
                 // Scale RGB without taking in account alpha
                TransformedBitmap scaledSrc = new TransformedBitmap(new FormatConvertedBitmap(src, PixelFormats.Bgr32, null, 0), scale);
                if (ReturnAlfa is true) return scaledSrc;

                // Merge them back
                return MergeAlphaAndRGB(scaledSrc, scaledAlpha);
            }
            else
            {
                return new TransformedBitmap(src, scale);
            }
        }






        private static BitmapSource CreateTransparency(BitmapSource source)
        {

            if (source.Format != PixelFormats.Bgra32)
            {
                return source;
            }

            int bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
            int stride = bytesPerPixel * source.PixelWidth;
            byte[]? buffer = new byte[stride * source.PixelHeight];

            source.CopyPixels(buffer, stride, 0);


            for (int y = 0;y < source.PixelHeight;y++)
            {
                for (int x = 0;x < source.PixelWidth;x++)
                {
                    int i = stride * y + bytesPerPixel * x;
                    byte blue = buffer[i];
                    byte green = buffer[i + 1];
                    byte red = buffer[i + 2];
                    byte alpha = buffer[i + 3];



                    if (alpha is 255)
                    {
                        buffer[i + 3] = 255;
                    }
                }

            }

            return BitmapSource.Create(
            source.PixelWidth, source.PixelHeight,
            source.DpiX, source.DpiY,
            source.Format, null, buffer, stride);

        }

        internal static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png); // Pit: When the format is Bmp, no transparency

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;

                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }



        internal static Uri GetUriApp(string resourcePath)
        {
            var uri = string.Format(
                "pack://application:,,,/{0};component/{1}"
                , Assembly.GetExecutingAssembly().GetName().Name
                , resourcePath
            );

            return new Uri(uri);
        }

        #endregion

        #region Сохранение изображения на диск
        /// <summary>
        ///     <br>Путь для сохранения <see cref="Uri"/> <paramref name="path"/></br>
        ///     <br> Изображение для сохранения <see cref="BitmapSource"/> <paramref name="Image"/></br>
        /// </summary>
        /// <returns>Успешность операции.</returns>
        internal static bool SafeImage(Uri Path, BitmapSource Image, string Name)
        {
            try
            {
                using (FileStream createFileFromImageBuffer = new FileStream(@$"{Path.AbsolutePath}{Name}.png", FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(Image));
                    encoder.Save(createFileFromImageBuffer);
                }
                return true;
            }
            catch (Exception) { return false; }
        }
        internal static bool SafeImage(Uri Path, BitmapSource Image)
        {
            try
            {
                using (FileStream createFileFromImageBuffer = new FileStream(@$"{Path.AbsolutePath}", FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(Image));
                    encoder.Save(createFileFromImageBuffer);
                }
                return true;
            }
            catch (Exception) { return false; }
        }
        #endregion

        #endregion

    }
}
