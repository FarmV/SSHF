using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using ControlzEx.Standard;

using FVH.SSHF.Infrastructure.Interfaces;

using R3;

namespace FVH.SSHF.Infrastructure.Input
{
    internal class WaitingInputProvider : IDisposable
    {
        private bool _isDisposed = false;
        private bool _isInstallHook = false;
        private bool _isInit = false;
        private readonly R3.BehaviorSubject<bool> _subjectRequestSwitchInput;
        private readonly Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> _subjectListGlobalShortcuts;
        private readonly Background.Input.Input _input;
        private readonly IDisposable _disposablesSubscribe;
        private readonly Dispatcher _dispatcher;

#if DEBUG
        internal readonly R3.BehaviorSubject<bool> CurrentStatusSubscribeInput;
#endif
        internal event FVH.Background.Input.CallbackFunctionKeyboard.LowLevelKeyboard.KeyboardEventHandler? NotifyKeyboardEvent;
        internal WaitingInputProvider(Dispatcher toCallbackDispatcher, R3.BehaviorSubject<bool> requestInputState, Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> listGlobalShortcutsAsObservable, SynchronizationContext workerContext)
        {
            _subjectRequestSwitchInput = requestInputState;
            _subjectListGlobalShortcuts = listGlobalShortcutsAsObservable;

            _dispatcher = toCallbackDispatcher;

            _input = new Background.Input.Input(toCallbackDispatcher);
            _input.NotifyKeyboardEvent += InputNotifyKeyboardEvent;
#if DEBUG
            CurrentStatusSubscribeInput = new R3.BehaviorSubject<bool>(false);
#endif
            IDisposable subscribeSetInput = _subjectRequestSwitchInput.SkipWhile((bool x) => x is true).// Не нужно вызывать UninstallHook раньше подписки, хотя там и защита - это логически неверно.
                ObserveOn(workerContext).Subscribe((bool requestSubOrUnSub) => InputRequestChecker(requestSubOrUnSub), onCompleted: (Result _) =>  Dispose());

            _disposablesSubscribe = R3.Disposable.Combine(subscribeSetInput);
        }      
        public void Dispose()
        {
            if(_isDisposed is true) return;
            _disposablesSubscribe.Dispose();
            _input.NotifyKeyboardEvent -= InputNotifyKeyboardEvent;
            _input?.Dispose();
#if DEBUG
            CurrentStatusSubscribeInput.OnCompleted(Result.Success);
            CurrentStatusSubscribeInput.Dispose();
#endif
            _isDisposed = true;
        }
        public void RegisterShortcuts()
        {
            ObjectDisposedException.ThrowIf(_isDisposed is true, this);
            if(_isInit is true) Throw(); [DoesNotReturn] static void Throw() => throw new InvalidOperationException("The object has already been initialized");
            _isInit = true;

            BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>? list = _subjectListGlobalShortcuts.Invoke();

            Task.Run(async () =>
            (await list.FirstAsync()).ToList().ForEach((IBehaviorSubjectGlobalShortcuts iGlobalShortcutBehaviorSubject) =>
            {
                R3.BehaviorSubject<IEnumerable<KeyboardShortcut>> shortcutsAsObservable = iGlobalShortcutBehaviorSubject.GetShortcutsAsObservable();
                IEnumerable<KeyboardShortcut> keyboardShortcutList = shortcutsAsObservable.FirstAsync().Result;
                keyboardShortcutList.ToList().ForEach((KeyboardShortcut keyboardShortcut) =>
                _input.AddCallbackTask(keyboardShortcut.KeyCombo.CurrentValue, keyboardShortcut.CallbackTask, keyboardShortcut.Identifier ?? keyboardShortcut.CallbackTask.Method.Name, keyboardShortcut.CanExecute).Wait());
            })).Wait();
        }
        private void InputNotifyKeyboardEvent(ref FVH.Background.Input.KeyboardEventArgs ev) => NotifyKeyboardEvent?.Invoke(ref ev);
        private void InputRequestChecker(bool requestUninstallHook)
        {                 
            ObjectDisposedException.ThrowIf(_isDisposed is true, this);
            
            switch(requestUninstallHook)
            {
                case false:
                            if(_isInstallHook is true) return;

                            _input.InstallHookToInputDispatcher();
                            
                            _isInstallHook = true;
#if DEBUG
                            _ = Task.Run(() => CurrentStatusSubscribeInput.OnNext(_isInstallHook));
#endif
                break;
                case true:
                           if(_isInstallHook is false) return;

                           _input.UninstallHookToInputDispatcher();
#if DEBUG                  
                           if(App.Stopwatch.ElapsedMilliseconds != 0) Debug.WriteLine($"{App.Stopwatch.ElapsedMilliseconds}");
#endif
                           _dispatcher.Invoke(() => { if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is true) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread(); });

                           _isInstallHook = false;
#if DEBUG                  
                            _ = Task.Run(() => CurrentStatusSubscribeInput.OnNext(_isInstallHook));
#endif                     
                break;
            }
        }     
    }
}