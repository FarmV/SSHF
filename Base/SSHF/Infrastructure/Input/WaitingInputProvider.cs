using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FVH.Background.Input;
using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Win32;

using R3;




namespace FVH.SSHF.Infrastructure.Input
{
    internal class WaitingInputProvider : IDisposable
    {
        private bool _isDisposed = false;
        private bool _statusHookInput = true;
        private readonly R3.BehaviorSubject<bool> _subjectRequestSwitchInput;
        private readonly Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> _subjectListGlobalShortcuts;
        private readonly Background.Input.Input _input;
        private readonly IDisposable _disposablesSubscribe;
        internal readonly R3.BehaviorSubject<bool> CurrentStatusSubscribeInput;
        internal event EventHandler<FVH.Background.Input.KeyboardEventArgs>? NotifyKeyboardEvent;

        internal WaitingInputProvider(Dispatcher toCallbackDispatcher, R3.BehaviorSubject<bool> setInputLifeAsObservable, Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> listGlobalShortcutsAsObservable)
        {
            _subjectRequestSwitchInput = setInputLifeAsObservable;
            _subjectListGlobalShortcuts = listGlobalShortcutsAsObservable;

            _input = new Background.Input.Input(toCallbackDispatcher);
            _input.NotifyKeyboardEvent += InputNotifyKeyboardEvent;

            CurrentStatusSubscribeInput = new R3.BehaviorSubject<bool>(false);
  
            IDisposable subscribeSetInput = _subjectRequestSwitchInput.ObserveOn(ObservableSystem.DefaultTimeProvider).
            Subscribe((bool requestSubOrUnSub) =>
            {
                InputRequestChecker(requestSubOrUnSub);
            },
            onCompleted: (Result _) => 
            {
                Dispose();
            });

            _disposablesSubscribe = R3.Disposable.Combine(subscribeSetInput);
        }      
        public void Dispose()
        {
            if(_isDisposed is true) return;
            _disposablesSubscribe.Dispose();
            _input.NotifyKeyboardEvent -= InputNotifyKeyboardEvent;
            _input?.Dispose();
            CurrentStatusSubscribeInput.OnCompleted(Result.Success);
            CurrentStatusSubscribeInput.Dispose();
            _isDisposed = true;
        }
        private void InputNotifyKeyboardEvent(object? sender, FVH.Background.Input.KeyboardEventArgs e) => NotifyKeyboardEvent?.Invoke(this, e);
        private bool _isInit = false;
        private void InputRequestChecker(bool statusRequestUnintsallHook)
        {
            void RegisterGlobalShortcuts(BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>> subject) =>            
            Task.Run(async () => 
            (await subject.FirstAsync()).ToList().ForEach((IBehaviorSubjectGlobalShortcuts iGlobalShortcutBehaviorSubject) =>
            {
                R3.BehaviorSubject<IEnumerable<KeyboardShortcut>> shortcutsAsObservable = iGlobalShortcutBehaviorSubject.GetShortcutsAsObservable();
                IEnumerable<KeyboardShortcut> keyboardShortcutList = shortcutsAsObservable.FirstAsync().Result;
                keyboardShortcutList.ToList().ForEach((KeyboardShortcut keyboardShortcut) =>
                _input.AddCallbackTask(keyboardShortcut.KeyCombo.CurrentValue, keyboardShortcut.CallbackTask, keyboardShortcut.Identifier ?? keyboardShortcut.CallbackTask.Method.Name).Wait());
            })).Wait();
           
            ObjectDisposedException.ThrowIf(_isDisposed is true, this);
            
            switch(statusRequestUnintsallHook)
            {
                case false:
                    if(_statusHookInput is true) return;
                    if(_isInit is false)
                    {
                        _isInit = true;
                        BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>? list = _subjectListGlobalShortcuts.Invoke();
                        RegisterGlobalShortcuts(list);
                    }

                    _input.InstallHook();

                    _statusHookInput = true;

                    CurrentStatusSubscribeInput.OnNext(_statusHookInput);   
                    break;
                case true:
                    if(_statusHookInput is false) return;
                   _input.UninstallHook();
#if DEBUG
                   Debug.WriteLine($"{App.Stopwatch.ElapsedMilliseconds}");
#endif
                   if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is true) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread();
                   _statusHookInput = false;
                   CurrentStatusSubscribeInput.OnNext(_statusHookInput);
                break;
            }
        }     
    }
}
