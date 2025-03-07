using System;
using System.Windows;
using System.Windows.Input;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Input;

using R3;


namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowExternalConditions : IDisposable
    {
        private bool _isDispose = false;
        private readonly FastWindowViewModel _mainWindowViewModel;
        private readonly R3.CompositeDisposable _disposables;
        private readonly IDisposable? _keyboardHandlerSubscription;
        private readonly WaitingInputProvider _provider;
        internal FastWindowExternalConditions(FastWindowViewModel mainWindowViewModel, WaitingInputProvider provider) //todo Позаботится об отписках
        {
            _mainWindowViewModel = mainWindowViewModel;
            _disposables = new R3.CompositeDisposable();

            _provider = provider;

            _provider.NotifyKeyboardEvent += NotifyKeyboardEvent;

            // _keyboardHandlerSubscription = keyboardHandler.Subscribe((IKeyboardHandler? iKeyboardHandler) => Subscribe(iKeyboardHandler));       
        }

        private void NotifyKeyboardEvent(object? _, Background.Input.KeyboardEventArgs e)
        {
            if(e.Key == VKeys.VK_LCONTROL)
            {
                if(_mainWindowViewModel.VisibleCondition.CurrentValue == Visibility.Hidden)
                {
                    bool IsReturn = true;
#if OneFastWindowNotTopMost
                               IsReturn = false;
#endif
                    if(IsReturn is true) return;
                }
                else
                {
                    if(e.Type == Background.Input.KeyboardEventArgs.TypePhysicallyEvent.Down)
                    {
                        if(_mainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is false)
                        {
                            _mainWindowViewModel.SetDragMoveCondition(false);
                            _mainWindowViewModel.SetDropCondition(true);
                        }
                    }
                    else
                    {
                        if(_mainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is false)
                        {
                            _mainWindowViewModel.SetDragMoveCondition(true);
                            _mainWindowViewModel.SetDropCondition(false);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = true;
            _provider.NotifyKeyboardEvent -= NotifyKeyboardEvent;
            _keyboardHandlerSubscription?.Dispose();
            _disposables?.Dispose();
        }
        //        private void Subscribe(IKeyboardHandler? keyboardHandler)
        //        {
        //            switch(keyboardHandler)
        //            {
        //                case not null:
        //                    R3.Observable<VKeys[]> keyPressObservable = R3.Observable.FromEventHandler(
        //                                        (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyPressEvent += handler,
        //                                        (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyPressEvent -= handler).Select(x => x.e.Keys);



        //                    R3.Observable<VKeys[]> keyUPObservable = R3.Observable.FromEventHandler(
        //                                         (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyUpPressEvent += handler,
        //                                         (EventHandler<IKeysNotifier> handler) => keyboardHandler.KeyUpPressEvent -= handler).Select(x => x.e.Keys);

        //                    IDisposable keyPressSubscribe = keyPressObservable.ObserveOn(ObservableSystem.DefaultTimeProvider).Subscribe(x =>
        //                       {
        //                           if(Keyboard.IsKeyUp(Key.LeftCtrl) is false)
        //                           {
        //                               _mainWindowViewModel.SetDragMoveCondition(false);
        //                               _mainWindowViewModel.SetDropCondition(true);
        //                           }
        //                       });

        //                       IDisposable keyUPSubscribe = keyUPObservable.ObserveOn(ObservableSystem.DefaultTimeProvider).Subscribe(x =>
        //                       {
        //                           if(_mainWindowViewModel.VisibleCondition.CurrentValue == Visibility.Hidden)
        //                           {
        //                               bool IsReturn = true;
        //#if OneFastWindowNotTopMost
        //                               IsReturn = false;
        //#endif
        //                               if(IsReturn is true) return;                                                                                          
        //                           }
        //                           if(Keyboard.IsKeyUp(Key.LeftCtrl) is true)
        //                           {
        //                               _mainWindowViewModel.SetDragMoveCondition(true);     
        //                               _mainWindowViewModel.SetDropCondition(false);
        //                           }
        //                       });
        //                    _disposables.Add(keyPressSubscribe);
        //                    _disposables.Add(keyUPSubscribe);
        //                break;
        //                case null:
        //                   _disposables.Clear();
        //                break;
        //            }
        //        }
        //    }
    }
}
