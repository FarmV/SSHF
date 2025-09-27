using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using FVH.SSHF;

using R3;

namespace FVH.SSHF.Infrastructure.Win32
{
    internal sealed partial class ObserverExclusiveMode : IDisposable
    {
        private bool                            _isDisposed       = false;
        private readonly SemaphoreSlim         _exclusiveModeLock = new SemaphoreSlim(1, 1);
        private readonly Dispatcher            _dispatcher;
        private readonly BehaviorSubject<bool> _exclusiveModeSubject;
        private CancellationTokenSource?       _exclusiveModeConfirmCts;
        public ReadOnlyReactiveProperty<bool> IsInExclusiveMode { get; }
        internal ObserverExclusiveMode(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            _exclusiveModeSubject = new BehaviorSubject<bool>(false);
            IsInExclusiveMode = _exclusiveModeSubject.ToReadOnlyReactiveProperty();
        }
        public async Task CheckAndSetStateExcusiveModeAsync()
        {
            if(_exclusiveModeLock.Wait(0) is false) return;

            if(Volatile.Read(ref _isDisposed) is true) return;

            try
            {
                bool wasInExclusiveMode = _exclusiveModeSubject.Value;
                bool isInExclusiveMode  = wasInExclusiveMode;

                isInExclusiveMode = D3DKMTCheckExclusiveOwnership();

                if(isInExclusiveMode != wasInExclusiveMode)
                {
                    _exclusiveModeSubject.OnNext(isInExclusiveMode);
                    for(int i = 0;i < 3;i++)
                    {
                        await Task.Delay(48);
                        bool retry = D3DKMTCheckExclusiveOwnership();
                        if(retry == isInExclusiveMode) continue;
                        isInExclusiveMode = retry;
                    }
                }
                else
                {
                    for(int i = 0;i < 3;i++)
                    {
                        await Task.Delay(48);
                        bool retry = D3DKMTCheckExclusiveOwnership();
                        if(retry == isInExclusiveMode) continue;
                        isInExclusiveMode = retry;
                    }
                }

                _exclusiveModeConfirmCts?.Cancel();
                _exclusiveModeConfirmCts = new CancellationTokenSource();
                CancellationToken token = _exclusiveModeConfirmCts.Token;

                _ = Task.Delay(48, token).ContinueWith(task =>
                {
                    if(task.IsCanceled) return;

                    bool exclusive = D3DKMTCheckExclusiveOwnership();
                    if(exclusive != _exclusiveModeSubject.Value)
                    {
                        if(exclusive is true) _ = _dispatcher.Invoke(Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread);

                        _exclusiveModeSubject.OnNext(exclusive);
                    }
                }, TaskScheduler.Default);

                if(isInExclusiveMode != wasInExclusiveMode)
                {
                    if(isInExclusiveMode is true) _ = _dispatcher.Invoke(Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread);

                    _exclusiveModeSubject.OnNext(isInExclusiveMode);
                }
            }
            finally { _ = _exclusiveModeLock.Release(); }
        }
        public void Dispose()
        {
            if(Volatile.Read(ref _isDisposed)) return;
            Volatile.Write(ref _isDisposed, true);

            _exclusiveModeSubject.OnCompleted();
            _exclusiveModeSubject.Dispose();

            _exclusiveModeLock.Dispose();
            IsInExclusiveMode.Dispose();
        }

        [LibraryImport("Gdi32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool D3DKMTCheckExclusiveOwnership();
    }
}