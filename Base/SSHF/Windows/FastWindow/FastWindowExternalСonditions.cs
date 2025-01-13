using System;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Linq;

using ReactiveUI;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;
using System.Reactive.Disposables;
using System.Reactive.Subjects;


namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowExternalConditions : IDisposable
    {
        private bool _isDispose = false;
        private readonly FastWindowViewModel _mainWindowViewModel;
        private readonly CompositeDisposable _disposables;
        private readonly IDisposable? _keyboardHandlerSubscription;
        internal FastWindowExternalConditions(FastWindowViewModel mainWindowViewModel, BehaviorSubject<IKeyboardHandler?> keyboardHandler) //todo Позаботится об отписках
        {
            _mainWindowViewModel = mainWindowViewModel;
            _disposables = new CompositeDisposable();

            _keyboardHandlerSubscription = keyboardHandler.Subscribe((IKeyboardHandler? iKeyboardHandler) => Subscribe(iKeyboardHandler));       
        }
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = true;
            _keyboardHandlerSubscription?.Dispose();
            _disposables?.Dispose();
        }
        private void Subscribe(IKeyboardHandler? keyboardHandler)
        {
            switch(keyboardHandler)
            {
                case not null:
                    IObservable<VKeys[]> keyPressObservable = Observable.FromEventPattern(
                                        (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyPressEvent += handler,
                                        (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyPressEvent -= handler).Select(x => x.EventArgs.Keys);
                    
                    IObservable<VKeys[]> keyUPObservable = Observable.FromEventPattern(
                                         (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyUpPressEvent += handler,
                                         (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyUpPressEvent -= handler).Select(x => x.EventArgs.Keys);
                    
                    IDisposable keyPressSubscribe = keyPressObservable.ObserveOn(RxApp.MainThreadScheduler).SubscribeOn(RxApp.MainThreadScheduler).Subscribe(x =>
                    {
                        if(Keyboard.IsKeyUp(Key.LeftCtrl) is false)
                        {
                            _mainWindowViewModel.DragMoveCondition = false;
                            _mainWindowViewModel.DropCondition = true;
                        }
                    });
                    
                    IDisposable keyUPSubscribe = keyUPObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
                    {
                        if(_mainWindowViewModel.VisibleCondition == Visibility.Hidden) return;
                        if(Keyboard.IsKeyUp(Key.LeftCtrl) is true)
                        {
                            _mainWindowViewModel.DragMoveCondition = true;
                            _mainWindowViewModel.DropCondition = false;
                        }
                    });
                    _disposables.Add(keyPressSubscribe);
                    _disposables.Add(keyUPSubscribe);
                break;
                case null:
                   _disposables.Clear();
                break;
            }
        }
    }
}
