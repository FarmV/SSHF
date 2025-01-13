using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Interfaces;

using ReactiveUI;


namespace FVH.SSHF.Infrastructure.Input
{
    internal class WaitingInputProvider : IDisposable
    {
        private bool _isDisposed = false;
        private bool _IsDisposeInternalInput = true;
        private readonly BehaviorSubject<bool> _subjectRequestSwitchInput;
        private readonly BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>> _subjectListGlobalShortcuts;
        private Background.Input.Input? _input;
        private readonly IDisposable _disposablesSubscribe;
        internal readonly BehaviorSubject<bool> IsDisposeInput;
        internal readonly BehaviorSubject<IKeyboardHandler?> CurrentInstanceIKeyboardHandlerOrDefault;
        internal WaitingInputProvider(BehaviorSubject<bool> setInputLifeAsObservable, BehaviorSubject<IEnumerable<IBehaviorSubjectGlobalShortcuts>> listGlobalShortcutsAsObservable)
        {
            _subjectRequestSwitchInput = setInputLifeAsObservable;
            _subjectListGlobalShortcuts = listGlobalShortcutsAsObservable;

            CurrentInstanceIKeyboardHandlerOrDefault = new BehaviorSubject<IKeyboardHandler?>(null);
            IsDisposeInput = new BehaviorSubject<bool>(true);

            IDisposable subscribeSetInput = _subjectRequestSwitchInput.ObserveOn(RxApp.MainThreadScheduler).Subscribe((requestDisposeInput) => InputRequestChecker(requestDisposeInput));

            _disposablesSubscribe = StableCompositeDisposable.Create(subscribeSetInput);

            _subjectRequestSwitchInput.Finally(Dispose);
            _subjectListGlobalShortcuts.Finally(Dispose);
        }
        public void Dispose()
        {
            if(_isDisposed is true) return;
            _disposablesSubscribe.Dispose();
            _input?.Dispose();
            IsDisposeInput.OnCompleted();
            IsDisposeInput.Dispose();
            _isDisposed = true;
        }
        private void InputRequestChecker(bool IsDisposeInput)
        {
            IKeyboardCallback? keyboardCallback;

            void RegisterGlobalShortcuts()
            {
                Task task1 = Task.Run(async () =>
                {
                    IEnumerable<IBehaviorSubjectGlobalShortcuts> r = await _subjectListGlobalShortcuts.FirstAsync();
                    foreach(IBehaviorSubjectGlobalShortcuts item in r)
                    {
                        BehaviorSubject<IEnumerable<KeyboardShortcut>> shortcutsAsObservable = item.GetShortcutsAsObservable();
                        IEnumerable<KeyboardShortcut> keyboardShortcutList = shortcutsAsObservable.FirstAsync().Wait();
                        foreach(KeyboardShortcut keyboardShortcut in keyboardShortcutList)
                        {
                            keyboardCallback.AddCallBackTask(keyboardShortcut.KeyCombo, keyboardShortcut.CallbackTask, keyboardShortcut.Identifier ?? keyboardShortcut.CallbackTask.Method.Name).Wait();
                        }
                    }
                });
                task1.Wait();
            }
            switch(IsDisposeInput)
            {
                case false:
                _input?.Dispose();
                _input = new Background.Input.Input();

                _IsDisposeInternalInput = false;
                keyboardCallback = _input.GetKeyboardCallbackFunction();

                CurrentInstanceIKeyboardHandlerOrDefault.OnNext(_input.GetKeyboardHandler());

                RegisterGlobalShortcuts();

                this.IsDisposeInput.OnNext(_IsDisposeInternalInput);
                break;
                case true:
                _input?.Dispose();
                _input = null;

                _IsDisposeInternalInput = true;
                CurrentInstanceIKeyboardHandlerOrDefault.OnNext(null);
                this.IsDisposeInput.OnNext(_IsDisposeInternalInput);
                break;
            }
        }
    }
}
