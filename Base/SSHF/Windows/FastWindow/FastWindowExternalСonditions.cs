using System;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Linq;

using ReactiveUI;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.ViewModels.MainWindowViewModel;

namespace FVH.SSHF.Windows.MainWindow
{
    internal class MainWindowExternalConditions
    {
        private readonly FastWindowViewModel _mainWindowViewModel;
        private readonly IKeyboardHandler _keyboardHandler;
        public MainWindowExternalConditions(FastWindowViewModel mainWindowViewModel, IKeyboardHandler keyboardHandler)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _keyboardHandler = keyboardHandler;
            Subscribe();
        }
        private void Subscribe()
        {
            IObservable<VKeys[]> keyPressObservable = Observable.FromEventPattern(
                                (EventHandler<IKeysNotifier> handler) => _keyboardHandler.KeyPressEvent += handler,
                                (EventHandler<IKeysNotifier>  handler) => _keyboardHandler.KeyPressEvent -= handler).Select(x => x.EventArgs.Keys);

            IObservable<VKeys[]> keyUPObservable = Observable.FromEventPattern(
                                 (EventHandler<IKeysNotifier> handler) => _keyboardHandler.KeyUpPressEvent += handler,
                                 (EventHandler<IKeysNotifier> handler) => _keyboardHandler.KeyUpPressEvent -= handler).Select(x => x.EventArgs.Keys);

            _ = keyPressObservable.ObserveOn(RxApp.MainThreadScheduler).SubscribeOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                if (Keyboard.IsKeyUp(Key.LeftCtrl) is false)
                {
                    _mainWindowViewModel.DragMoveCondition = false;
                    _mainWindowViewModel.DropCondition = true;
                }
            });

            _ = keyUPObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                if (_mainWindowViewModel.VisibleCondition == Visibility.Hidden) return;
                if (Keyboard.IsKeyUp(Key.LeftCtrl) is true)
                {
                    _mainWindowViewModel.DragMoveCondition = true;
                    _mainWindowViewModel.DropCondition = false;
                }
            });
        }
    }
}
