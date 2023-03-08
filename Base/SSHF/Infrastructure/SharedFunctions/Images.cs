using System;
using System.Collections.Generic;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;



using Color = System.Drawing.Color;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal static class Images
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
    }
}
