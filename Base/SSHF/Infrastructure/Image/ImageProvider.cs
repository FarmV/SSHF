using SSHF.Infrastructure.Interfaces;

using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SSHF.Infrastructure
{
    internal class ImageProvider : IGetImage
    {
        public Task<ImageSource?> GetImageFromFile(Uri path) => Task.FromResult<ImageSource?>(ImageFromFile.GetBitmapImage(path));
        public async Task<ImageSource?> GetImageFromClipboard() => await ImageFromClipboard.GetClipboardImage();
    }
}


