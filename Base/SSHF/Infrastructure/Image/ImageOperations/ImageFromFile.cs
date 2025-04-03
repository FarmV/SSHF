using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FVH.SSHF.Infrastructure
{
    internal static class ImageFromFile
    {
        internal static BitmapImage? GetBitmapImage(Uri path)
        {
            try
            {
                BitmapImage Image = new BitmapImage();
                Image.BeginInit();
                Image.UriSource = path;
                Image.CacheOption = BitmapCacheOption.OnLoad;  // Инициализаця - файл был в памяти и не занят
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


