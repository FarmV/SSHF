using FVH.SSHF.Infrastructure.Interfaces;

using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FVH.SSHF.Infrastructure
{
    public class ImageProvider 
    {
        public Task<ImageSource?> GetImageFromFile(Uri path) => Task.FromResult<ImageSource?>(ImageFromFile.GetBitmapImage(path));
        public async Task<ImageSource?> GetImageFromClipboard() => await ImageFromClipboard.GetClipboardImage();
    }
}


