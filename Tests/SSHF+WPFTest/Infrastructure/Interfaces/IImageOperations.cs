using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    internal interface IImageOperations
    {
        enum ImageType
        {
            SystemDrawingBitmap,
            SystemWindowsMediaImageSource,
            SystemWindowsMediaImagingBitmapSource,
            SystemWindowsMediaImagingBitmapImage,
        }
        //virtual void test()
        //{
        //    System.Drawing.Bitmap a;
        //    System.Windows.Media.ImageSource d;
        //    System.Windows.Media.Imaging.BitmapSource c;
        //    System.Windows.Media.Imaging.BitmapImage b;

        //}

        abstract Task<Tuple<bool, object?, string>> ConvetImage(ImageType imageInput, ImageType imageOutput, object image);

        abstract bool ScaleImage(ImageType imageInput, ImageType imageOutput, double sacle);

    }
}
