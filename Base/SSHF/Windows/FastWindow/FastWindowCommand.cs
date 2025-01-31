using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
            Task SetNewImage()
            {
                if(MainWindowViewModel.SetNewImage.CanExecute() is false) return Task.CompletedTask;
                MainWindowViewModel.SetNewImage.Execute(R3.Unit.Default);
                return Task.CompletedTask;          
            }
            Task ShowWindow()
            {
                if(MainWindowViewModel.ShowWindow.CanExecute() is false) return Task.CompletedTask;
                MainWindowViewModel.ShowWindow.Execute(R3.Unit.Default);
                return Task.CompletedTask;
            }
            Task RefreshWindowInvoke()
            {
                
                if(MainWindowViewModel.RefreshWindowInvoke.CanExecute() is false) return Task.CompletedTask;
                MainWindowViewModel.RefreshWindowInvoke.Execute(R3.Unit.Default);
                return Task.CompletedTask;
            }
            if(_isExecutePresentNewImage is true) return;
            try
            {
                _isExecutePresentNewImage = true;

                await SetNewImage().ConfigureAwait(false);
                await ShowWindow().ConfigureAwait(false);

                await RefreshWindowInvoke().ConfigureAwait(false);
            }
            finally { Volatile.Write(ref _isExecutePresentNewImage, false); }
        }
        public Task SwitchBlockRefreshWindow()
        {
            if(MainWindowViewModel.SwitchBlockRefreshWindow.CanExecute() is false) return Task.CompletedTask;
            MainWindowViewModel.SwitchBlockRefreshWindow.Execute(R3.Unit.Default);
            return Task.CompletedTask;
        }
        public Task StopRefreshWindow()
        {
            if(MainWindowViewModel.StopWindowUpdater.CanExecute() is false) return Task.CompletedTask;
            MainWindowViewModel.StopWindowUpdater.Execute(R3.Unit.Default);
            return Task.CompletedTask;            
        }
        public Task InvokeMsScreenClip()
        {
            if(MainWindowViewModel.MsScreenClipInvoke.CanExecute() is false) return Task.CompletedTask;
            MainWindowViewModel.MsScreenClipInvoke.Execute(R3.Unit.Default);
            return Task.CompletedTask;
        }
        public Task HideWindow()
        {
            if(MainWindowViewModel.HideWindow.CanExecute() is false) return Task.CompletedTask;
            MainWindowViewModel.HideWindow.Execute(R3.Unit.Default);
            return Task.CompletedTask; 
        }        
    }
}
