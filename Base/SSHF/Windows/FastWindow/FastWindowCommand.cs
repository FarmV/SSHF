using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure.Interfaces;

namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowCommand 
    {
        private readonly System.Windows.Window _window;
        private bool _isExecutePresentNewImage = false;
        private KeyboardShortcut[]? _shortcuts;
        internal FastWindowCommand(System.Windows.Window window, FastWindowViewModel mainWindowViewModel)
        {
            _window = window;
            MainWindowViewModel = mainWindowViewModel;
            SetNewShortcuts(GetDefaultShortcuts());
        }
        internal void SetNewShortcuts(KeyboardShortcut[] shortcuts) => _shortcuts = shortcuts;
        internal KeyboardShortcut[] GetDefaultShortcuts() =>
        [
            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            ],
            new Func<Task>(PresentNewImage), nameof(PresentNewImage)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_S
            ],
            new Func<Task>(InvokeMsScreenClip), nameof(InvokeMsScreenClip)),

            new KeyboardShortcut(
            [
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            ],
            new Func<Task>(SwitchBlockRefreshWindow), nameof(SwitchBlockRefreshWindow)),

            new KeyboardShortcut(
            [
                VKeys.VK_CONTROL
            ],
            new Func<Task>(StopRefreshWindow), nameof(StopRefreshWindow)),

            new KeyboardShortcut(
            [
                VKeys.VK_SCROLL
            ],
            new Func<Task>(InvokeMsScreenClip),$"SCROLL_{nameof(InvokeMsScreenClip)}"),
        ];
        public FastWindowViewModel MainWindowViewModel { get; }
        public IEnumerable<KeyboardShortcut> GetShortcuts() => _shortcuts ?? throw new NullReferenceException(nameof(_shortcuts));
        public async Task PresentNewImage()
        {
            async Task SetNewImage() =>
            _ = await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.SetNewImage.Execute().ToTask()).Task.Unwrap();
            async Task ShowWindow() =>
            _ = await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.ShowWindow.Execute().ToTask()).Task.Unwrap();
            async Task<bool> CanExecuteRefreshWindowInvoke() => await MainWindowViewModel.RefreshWindowInvoke.CanExecute.FirstAsync();       
            async Task RefreshWindowInvoke() => 
            _ = await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.RefreshWindowInvoke.Execute().ToTask()).Task.Unwrap();

            if(_isExecutePresentNewImage is true) return;
            try
            {
                _isExecutePresentNewImage = true;

                await _window.Dispatcher.InvokeAsync(SetNewImage).Task.Unwrap();
                await _window.Dispatcher.InvokeAsync(ShowWindow).Task.Unwrap();

                if(await CanExecuteRefreshWindowInvoke() is true) await _window.Dispatcher.InvokeAsync(RefreshWindowInvoke).Task.Unwrap();
            }
            finally { _isExecutePresentNewImage = false; }
        }
        public async Task SwitchBlockRefreshWindow() => 
        _ = await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.SwitchBlockRefreshWindow.Execute().ToTask()).Task.Unwrap();
        public async Task StopRefreshWindow() => 
        _ = MainWindowViewModel.WindowPositionUpdater.IsUpdateWindow ?
        await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.StopWindowUpdater.Execute().ToTask()).Task.Unwrap() : 
        await Task.FromResult(System.Reactive.Unit.Default);
        public async Task InvokeMsScreenClip() => 
        _ = await _window.Dispatcher.InvokeAsync(() => MainWindowViewModel.MsScreenClipInvoke.Execute().ToTask()).Task.Unwrap();         
    }
}
