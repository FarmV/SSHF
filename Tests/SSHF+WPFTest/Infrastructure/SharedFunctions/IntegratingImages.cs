using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                TransformedBitmap? Transformed = new TransformedBitmap(ImageToScale, new ScaleTransform(1.05, 1.05)); // Ваш Sacle
                                         
                using MemoryStream outStream = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                
                enc.Frames.Add(BitmapFrame.Create(Transformed));
                enc.Save(outStream);                  

                Bitmap bitmap = new Bitmap(outStream);
                
                bitmap.MakeTransparent(System.Drawing.Color.FromArgb(255));

                BitmapImage image = BitmapToBitmapImage(bitmap);

                return image;

            }
            catch (Exception)
            {
                BitmapImage? ImageNull = null;

                return ImageNull;
            }
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
        #endregion

        #endregion

    }
}
