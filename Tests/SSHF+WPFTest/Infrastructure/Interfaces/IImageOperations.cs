using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Interfaces
{
    internal interface IImageOperations
    {
        public event Action<object?> Сompleted;

        enum ImageType
        {
            SystemDrawingBitmap,
            SystemWindowsMediaImagingImageSourece,
            SystemWindowsMediaImagingImageSoureceBitmapSource
        }

        abstract void ConvetImage(ImageType imageInput, ImageType imageOutput, object image);

        abstract void ScaleImage(double sacle);

    }
}
