using System;
using System.Threading;
using System.Windows.Threading;

using R3;


namespace FVH.SSHF.Infrastructure.Win32
{
    using R3;

    using System;
    using System.Threading;
    using System.Windows.Threading;

    namespace FVH.SSHF.Infrastructure.Win32
    {
        internal sealed class ObserverExclusiveMode : IDisposable
        {
            private bool _isDisposed = false;
            private readonly Win32ExclusiveModeChecker _exclusiveModeChecker;
            private readonly Dispatcher _dispatcher;
            private readonly BehaviorSubject<bool> _exclusiveModeSubject;
            public ReadOnlyReactiveProperty<bool> IsInExclusiveMode { get; }
            internal ObserverExclusiveMode(Dispatcher dispatcher)
            {
                _dispatcher = dispatcher;
                _exclusiveModeChecker = _dispatcher.Invoke(() => new Win32ExclusiveModeChecker());

                _exclusiveModeSubject = new BehaviorSubject<bool>(false);
                IsInExclusiveMode = _exclusiveModeSubject.ToReadOnlyReactiveProperty();

                ArgumentNullException.ThrowIfNull(_exclusiveModeChecker);
            }
            internal void CheckAndSetStateExcusiveMode()
            {
                bool wasInExclusiveMode = _exclusiveModeSubject.Value;
                bool isInExclusiveMode;

                if(wasInExclusiveMode is false)
                {
                    TimeSpan empiricalTimeoutSpinWait = TimeSpan.FromMilliseconds(25);
                  
                    _ =  SpinWait.SpinUntil(() =>
                    {
                        isInExclusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher);
                        return isInExclusiveMode;
                    }, empiricalTimeoutSpinWait);

                    isInExclusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher);
                }
                else
                {
                    isInExclusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher);
                }
             
                if(isInExclusiveMode != wasInExclusiveMode)
                {
                    if(isInExclusiveMode)
                    {
                        _ = _dispatcher.Invoke(Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread);
                    }

                    _exclusiveModeSubject.OnNext(isInExclusiveMode);
                }
            }
            public void Dispose()
            {
                if(_isDisposed) return;
                _isDisposed = true;

                _exclusiveModeSubject.OnCompleted(); 
                _exclusiveModeSubject.Dispose();

                IsInExclusiveMode.Dispose();

                _dispatcher.Invoke(() => _exclusiveModeChecker.Dispose());
            }
        }
    }
}