using System;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using System.Reactive;
using FVH.Background.Input;
using System.Reactive.Linq;
using System.Linq;
using SSHF.Infrastructure.Interfaces;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal class MainWindowCommand : IInvokeShortcuts
    {
        private readonly System.Windows.Window _window;
        private readonly IKeyboardHandler _keyboardHandler;
        private bool _executePresentNewImage = false;
        public MainWindowCommand(System.Windows.Window window, MainWindowViewModel mainWindowView, IKeyboardHandler keyboardHandler)
        {
            _window = window;
            MainWindowViewModel = mainWindowView;
            _keyboardHandler = keyboardHandler;

            IObservable<VKeys[]> keyPressObservable = Observable.FromEventPattern(
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyPressEvent += handler,
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyPressEvent -= handler).Select(x => x.EventArgs.Keys);


            IObservable<VKeys[]> keyUPObservable = Observable.FromEventPattern(
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyUpPressEvent += handler,
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyUpPressEvent -= handler).Select(x => x.EventArgs.Keys);

            keyPressObservable.ObserveOn(RxApp.MainThreadScheduler).SubscribeOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Keyboard.IsKeyUp(Key.LeftCtrl) is false)
                    {
                        this.MainWindowViewModel.DragMoveCondition = false;
                        MainWindowViewModel.DropCondition = true;
                    }
                });
            });
            keyUPObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x => // todo разобратся почему приходит пустой эвент
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (this.MainWindowViewModel.VisibleCondition == Visibility.Hidden) return;
                    if (Keyboard.IsKeyUp(Key.LeftCtrl) is true)
                    {
                        this.MainWindowViewModel.DragMoveCondition = true;
                        MainWindowViewModel.DropCondition = false;
                    }
                });
            });
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
            new Func<Task>(PresentNewImage), nameof(MainWindowCommand.PresentNewImage)),

            new Shortcuts(
            [
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            ],
            new Func<Task>(SwithBlockRefreshWindow), nameof(MainWindowCommand.SwithBlockRefreshWindow)),

            new Shortcuts(
            [
                VKeys.VK_CONTROL
            ],
            new Func<Task>(StopRefreshWindow), nameof(MainWindowCommand.StopRefreshWindow))
        };
        public async Task SwithBlockRefreshWindow() => await Application.Current.Dispatcher.InvokeAsync(async () => await MainWindowViewModel.SwithBlockRefreshWindow.Execute().FirstAsync());
        public async Task PresentNewImage()
        {
            if (_executePresentNewImage is true) return;
            try
            {
                _executePresentNewImage = true;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await MainWindowViewModel.SetNewImage.Execute().FirstAsync();
                    await MainWindowViewModel.ShowWindow.Execute().FirstAsync();

                    _ = await this.MainWindowViewModel.RefreshWindowInvoke.CanExecute.FirstAsync() is true ? await MainWindowViewModel.RefreshWindowInvoke.Execute().FirstAsync() : Unit.Default;
                }).Task.ConfigureAwait(false);
            }
            finally { _executePresentNewImage = false; }
        }
        public async Task StopRefreshWindow()
        {
            if (MainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is true)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await MainWindowViewModel.StopWindowUpdater.Execute().FirstAsync());
            }
        }
    }
}
