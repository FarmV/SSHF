using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Win32;

using R3;

using static FVH.Background.Input.Input;


namespace FVH.SSHF.Infrastructure.Input
{
    internal class WaitingInputProvider : IDisposable
    {
        private bool _isDisposed = false;
        private bool _IsDisposeInternalInput = true;
        private readonly R3.BehaviorSubject<bool> _subjectRequestSwitchInput;
        private readonly Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> _subjectListGlobalShortcuts;
        private Background.Input.Input? _input;
        private readonly IDisposable _disposablesSubscribe;
        internal readonly R3.BehaviorSubject<bool> IsDisposeInput;
        internal readonly R3.BehaviorSubject<IKeyboardHandler?> CurrentInstanceIKeyboardHandlerOrDefault;
        internal WaitingInputProvider(R3.BehaviorSubject<bool> setInputLifeAsObservable, Func<R3.BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>> listGlobalShortcutsAsObservable)
        {
            _subjectRequestSwitchInput = setInputLifeAsObservable;
            _subjectListGlobalShortcuts = listGlobalShortcutsAsObservable;

            CurrentInstanceIKeyboardHandlerOrDefault = new R3.BehaviorSubject<IKeyboardHandler?>(null);
            IsDisposeInput = new R3.BehaviorSubject<bool>(true);

            IDisposable subscribeSetInput = _subjectRequestSwitchInput.ObserveOn(ObservableSystem.DefaultTimeProvider).
               Subscribe((bool requestDisposeInput) =>
               {
                 InputRequestChecker(requestDisposeInput);
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
            _input?.Dispose();
            IsDisposeInput.OnCompleted(Result.Success);
            IsDisposeInput.Dispose();
            _isDisposed = true;
        }
        private void InputRequestChecker(bool isDisposeInput)
        {
            ObjectDisposedException.ThrowIf(_isDisposed is true, this);

            IKeyboardCallback? keyboardCallback;

            void RegisterGlobalShortcuts(BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>> subject) =>            
            Task.Run(async () => 
            (await subject.FirstAsync()).ToList().ForEach((IBehaviorSubjectGlobalShortcuts iGlobalShortcutBehaviorSubject) =>
            {
                R3.BehaviorSubject<IEnumerable<KeyboardShortcut>> shortcutsAsObservable = iGlobalShortcutBehaviorSubject.GetShortcutsAsObservable();
                IEnumerable<KeyboardShortcut> keyboardShortcutList = shortcutsAsObservable.FirstAsync().Result;
                keyboardShortcutList.ToList().ForEach((KeyboardShortcut keyboardShortcut) =>
                keyboardCallback.AddCallBackTask(keyboardShortcut.KeyCombo.CurrentValue, keyboardShortcut.CallbackTask, keyboardShortcut.Identifier ?? keyboardShortcut.CallbackTask.Method.Name).Wait());
            })).Wait();
            
            switch(isDisposeInput)
            {
                case false:           
                    _input?.Dispose();
                    _input = new Background.Input.Input(initInputHandle: HandlersInput.Keyboard); // не забывать HandlersInput.Keyboard

                    _IsDisposeInternalInput = false;
                    keyboardCallback = _input.GetKeyboardCallbackFunction();
                    
                    CurrentInstanceIKeyboardHandlerOrDefault.OnNext(_input.GetKeyboardHandler());

                    BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>>? test = _subjectListGlobalShortcuts.Invoke();

                    RegisterGlobalShortcuts(test);
                    
                    this.IsDisposeInput.OnNext(_IsDisposeInternalInput);
                    break;
                case true:
                   _input?.Dispose();
#if DEBUG
                Debug.WriteLine($"{App.Stopwatch.ElapsedMilliseconds}");
#endif
                if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is true) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread();
                   _input = null;
                   _IsDisposeInternalInput = true;
                   CurrentInstanceIKeyboardHandlerOrDefault.OnNext(null);
                   this.IsDisposeInput.OnNext(_IsDisposeInternalInput);
                break;
            }
        }     
    }
}
