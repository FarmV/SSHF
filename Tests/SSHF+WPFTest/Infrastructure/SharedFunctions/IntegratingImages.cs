using System;
using System.Collections.Generic;
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


        internal static Uri GetUri(string resourcePath)
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
        internal static bool SafeImage(Uri path, BitmapSource Image)
        {
            try
            {
                using (FileStream createFileFromImageBuffer = new FileStream(path.AbsolutePath, FileMode.OpenOrCreate))
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
