using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.ViewModels.MainWindowViewModel
{
    internal class MainWindowCommand : IInvokeShortcuts
    {
        private readonly System.Windows.Window _window;
        private bool _isExecutePresentNewImage = false;
        internal MainWindowCommand(System.Windows.Window window, MainWindowViewModel mainWindowView)
        {
            _window = window;
            MainWindowViewModel = mainWindowView;        
        }
        public MainWindowViewModel MainWindowViewModel { get; }
        public IEnumerable<Shortcuts> GetShortcuts() => new Shortcuts[]
        {
            new Shortcuts(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            ],
            new Func<Task>(PresentNewImage), nameof(PresentNewImage)),

            new Shortcuts(
            [
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            ],
            new Func<Task>(SwitchBlockRefreshWindow), nameof(SwitchBlockRefreshWindow)),

            new Shortcuts(
            [
                VKeys.VK_CONTROL
            ],
            new Func<Task>(StopRefreshWindow), nameof(StopRefreshWindow)),

            new Shortcuts(
            [
                VKeys.VK_SCROLL
            ],
            new Func<Task>(InvokeMsScreenClip), nameof(InvokeMsScreenClip)),
        };
        public async Task PresentNewImage()
        {
            if(_isExecutePresentNewImage is true) return;
            try
            {
                _isExecutePresentNewImage = true;

                await _window.Dispatcher.InvokeAsync(SetNewImage);
                await _window.Dispatcher.InvokeAsync(ShowWindow);

                if(await CanExecuteRefreshWindowInvoke() is true) await _window.Dispatcher.InvokeAsync(RefreshWindowInvoke).Task.ConfigureAwait(false);
            }
            finally { _isExecutePresentNewImage = false; }
        }
        private async Task SetNewImage() => await MainWindowViewModel.SetNewImage.Execute().FirstAsync();
        private async Task ShowWindow() => await MainWindowViewModel.ShowWindow.Execute().FirstAsync();
        private async Task<bool> CanExecuteRefreshWindowInvoke() => await MainWindowViewModel.RefreshWindowInvoke.CanExecute.FirstAsync();
        private async Task RefreshWindowInvoke() => await MainWindowViewModel.RefreshWindowInvoke.Execute().FirstAsync();

        public async Task SwitchBlockRefreshWindow() => await _window.Dispatcher.InvokeAsync(BlockRefreshWindowSwitch);
        private async Task BlockRefreshWindowSwitch() => await MainWindowViewModel.SwitchBlockRefreshWindow.Execute().FirstAsync();

        public async Task StopRefreshWindow() { if(MainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is true) await _window.Dispatcher.InvokeAsync(RefreshWindowStop); }
        private async Task RefreshWindowStop() => await MainWindowViewModel.StopWindowUpdater.Execute().FirstAsync();

        public async Task InvokeMsScreenClip() => await _window.Dispatcher.InvokeAsync(MsScreenClipInvoke);
        private async Task MsScreenClipInvoke() => await MainWindowViewModel.MsScreenClipInvoke.Execute().FirstAsync();
    }
}
