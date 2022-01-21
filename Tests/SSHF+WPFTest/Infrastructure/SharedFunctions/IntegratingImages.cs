using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class IntegratingImages
    {
        #region Интеграция изображений

        #region Извлечение изображения из буфера обмена
        /// <returns>Возращает изображание, помещённое в память, из буфера обмена. Или пустую ссылку.</returns>
        BitmapSource? GetBufferImage()
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
        BitmapImage SetImageToMemoryFromDrive(Uri path)
        {

            if (System.IO.File.Exists(path.AbsolutePath))
            {
                BitmapImage Image = new BitmapImage();              // Инициализаци что бы файл был в памяти и не заянят
                Image.BeginInit();
                Image.UriSource = path;
                Image.CacheOption = BitmapCacheOption.OnLoad;
                Image.EndInit();

                return Image;
            }
            else throw new ArgumentException("SetImage Fail", nameof(path));
        }
        #endregion

        #region Сохранение изображения на диск
        /// <summary>
        ///     <br>Путь для сохранения <see cref="Uri"/> <paramref name="path"/></br>
        ///     <br> Изображение для сохранения <see cref="BitmapSource"/> <paramref name="Image"/></br>
        /// </summary>
        /// <returns>Успешность операции.</returns>
        bool SafeImage(Uri path, BitmapSource Image)
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
