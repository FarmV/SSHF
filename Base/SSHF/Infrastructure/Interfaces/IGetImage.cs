using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SSHF.Infrastructure.Interfaces
{
    public interface IGetImage
    {
        Task<ImageSource?> GetImageFromClipboard();
        Task<ImageSource?> GetImageFromFile(Uri path);
    }
}

