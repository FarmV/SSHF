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
        private bool _isHookActive = false;
        private bool _isInit = false;
        private readonly Observable<bool> _hookCanBeActive;
        private readonly Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> _subjectListGlobalShortcuts;
        private readonly Background.Input.Input _input;
        private readonly IDisposable _subscription;
        private readonly Dispatcher _dispatcher;

        internal event FVH.Background.Input.CallbackFunctionKeyboard.LowLevelKeyboard.KeyboardEventHandler? NotifyKeyboardEvent;
        internal WaitingInputProvider(Dispatcher toCallbackDispatcher, Observable<bool> hookCanBeActive, Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> listGlobalShortcutsAsObservable, SynchronizationContext workerContext)
        {
            _hookCanBeActive = hookCanBeActive;
            _subjectListGlobalShortcuts = listGlobalShortcutsAsObservable;

            _dispatcher = toCallbackDispatcher;

            _input = new Background.Input.Input(toCallbackDispatcher);
            _input.NotifyKeyboardEvent += InputNotifyKeyboardEvent;

            _subscription =  _hookCanBeActive.ObserveOn(workerContext).Subscribe((bool shouldBeActive) => UpdateHookActivity(shouldBeActive),onCompleted: (Result _) => Dispose());

        }      
        public void Dispose()
        {
            if(_isDisposed is true) return;
            _isDisposed = true;

            _subscription.Dispose();
            _input.NotifyKeyboardEvent -= InputNotifyKeyboardEvent;
            _input?.Dispose();
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
        private void UpdateHookActivity(bool shouldBeActive)
        {                 
            ObjectDisposedException.ThrowIf(_isDisposed is true, this);

            if(shouldBeActive == _isHookActive) return;

            if(shouldBeActive) 
            {
                _input.InstallHookToInputDispatcher();
                _isHookActive = true;
            }
            else 
            {
                _input.UninstallHookToInputDispatcher();

                _dispatcher.Invoke(() => { if(Thread.CurrentThread.InUIThreadTimeCriticalSection()) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread(); });
                _isHookActive = false;
            }
        }
    }     
}
