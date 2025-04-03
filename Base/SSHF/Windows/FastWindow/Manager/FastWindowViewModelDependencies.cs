using System;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowViewModelDependencies : IDisposable
    {
        internal bool IsDisposed = false;
        private IGetImage _iGetImage;
        private IWindowPositionUpdater? _iWindowPositionUpdater;
        private WPFDpiCorrector? _dpiCorrector;
        private WPFDropImageFile? _setImage;

        internal FastWindowViewModelDependencies(IGetImage imageProvider)
        {
            _iGetImage = imageProvider;
        }
        internal IGetImage IGetImage { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _iGetImage; } init => _iGetImage = value; }
        internal IWindowPositionUpdater? IWindowPositionUpdater { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _iWindowPositionUpdater; } set => _iWindowPositionUpdater = value; }
        internal WPFDpiCorrector? DpiCorrector { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _dpiCorrector; } set => _dpiCorrector = value; }
        internal WPFDropImageFile? SetImage { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _setImage; } set => _setImage = value; }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            SetImage?.Dispose();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

