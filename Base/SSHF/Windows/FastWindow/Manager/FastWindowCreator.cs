using System;
using System.Windows.Threading;
using System.Threading.Tasks;

using FVH.SSHF.Infrastructure;

namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowCreator
    {
        private readonly Dispatcher _dispatcher;
        private readonly Func<FastWindowViewModelDependencies> _getFastWindowViewModelDependencies;
        internal FastWindowCreator(Dispatcher UiDispatcher, Func<FastWindowViewModelDependencies> fastWindowViewModelDependencies)
        {
            ArgumentNullException.ThrowIfNull(UiDispatcher);
            _dispatcher = UiDispatcher;
            _getFastWindowViewModelDependencies = fastWindowViewModelDependencies;
        }
        internal async Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> CreateFastWindowAsync()
        {
            FastWindow window = await _dispatcher.InvokeAsync(() =>
            {
                return new FastWindow();
            });
            await _dispatcher.InvokeAsync(window.Show);
            FastWindowViewModelDependencies fastWindowViewModelDependencies = _getFastWindowViewModelDependencies.Invoke();

            fastWindowViewModelDependencies.DpiCorrector = new WPFDpiCorrector(window, _dispatcher);
            fastWindowViewModelDependencies.SetImage = new WPFDropImageFile(window);
            fastWindowViewModelDependencies.PositionManager = new Win32WPFWindowPositionManager(window, fastWindowViewModelDependencies.DpiCorrector);

            FastWindowViewModel viewModel = await _dispatcher.InvokeAsync(() => CreateViewModelFastWindow(fastWindowViewModelDependencies));
            await _dispatcher.InvokeAsync(() =>
            {
                window.DataContext = viewModel;
                window.ViewModel = viewModel;
            });
            return (window, viewModel, fastWindowViewModelDependencies);
        }
        private FastWindowViewModel CreateViewModelFastWindow(FastWindowViewModelDependencies fastWindowViewModelDependencies) =>
        _dispatcher.Invoke(() =>
            new FastWindowViewModel
            (
                fastWindowViewModelDependencies.ImageProvider,
                fastWindowViewModelDependencies.PositionManager!,
                fastWindowViewModelDependencies.DpiCorrector!,
                fastWindowViewModelDependencies.SetImage!,
                fastWindowViewModelDependencies.MsScreenClip!
            )
        );
    }
}

