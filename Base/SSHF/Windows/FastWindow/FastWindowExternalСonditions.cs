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
        private readonly WaitingInputProvider _provider;
        internal FastWindowExternalConditions(FastWindowViewModel mainWindowViewModel, WaitingInputProvider provider)
        {
            _mainWindowViewModel = mainWindowViewModel;

            _provider = provider;

            _provider.NotifyKeyboardEvent += NotifyKeyboardEvent;
   
        }
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = true;
            _provider.NotifyKeyboardEvent -= NotifyKeyboardEvent;
        }
        private void NotifyKeyboardEvent(ref Background.Input.KeyboardEventArgs e)
        {
            if(e.Type == Background.Input.KeyboardEventArgs.TypePhysicallyEvent.ForceClearState)
            {
                _mainWindowViewModel.SetDragMoveCondition(true);
                _mainWindowViewModel.SetDropCondition(false);
                return;
            }                      
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
                        if(_mainWindowViewModel.PositionManager.IsUpdateWindow is false)
                        {
                            _mainWindowViewModel.SetDragMoveCondition(false);
                            _mainWindowViewModel.SetDropCondition(true);
                        }
                    }
                    else
                    {
                        if(_mainWindowViewModel.PositionManager.IsUpdateWindow is false)
                        {
                            _mainWindowViewModel.SetDragMoveCondition(true);
                            _mainWindowViewModel.SetDropCondition(false);
                        }
                    }
                }
            }
        }
    }
}