using System;
using System.Threading;
using System.Windows.Threading;

using R3;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class ObserverExclusiveMode : IDisposable
    {
        private bool _isDispose = false;
        private bool _isExcusiveMode = false;
        internal Win32ExclusiveModeChecker _exclusiveModeChecker;
        internal readonly R3.BehaviorSubject<bool> ExcusiveMode;
        private readonly Dispatcher _dispatcher;
        internal ObserverExclusiveMode(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _exclusiveModeChecker = _dispatcher.Invoke(()=> _ = new Win32ExclusiveModeChecker());

            ExcusiveMode = new R3.BehaviorSubject<bool>(false);

            ArgumentNullException.ThrowIfNull(_exclusiveModeChecker);
        }
        public void Dispose()
        {
            if(_isDispose) return;
            _isDispose = true;
            ExcusiveMode.OnCompleted(Result.Success);
            ExcusiveMode.Dispose();
            _dispatcher.Invoke(() => _exclusiveModeChecker.Dispose());
        }
        internal void CheckAndSetStateExcusiveMode()
        {
            bool isExcusiveMode = false;
            if(ExcusiveMode.Value == false)
            {
                TimeSpan empiricalTimeoutSpinWait = TimeSpan.FromMilliseconds(25); // Предполагаемая задержка между получение фокуса окна и установкой режима
                _ = SpinWait.SpinUntil(() =>
                {
                    isExcusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher);
                    return isExcusiveMode is true;
                }, empiricalTimeoutSpinWait);
            }
            else { isExcusiveMode = _exclusiveModeChecker.CheckExclusiveMode(_dispatcher); }

            _isExcusiveMode = isExcusiveMode;
            if(_isExcusiveMode is true) { if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is false) _ = Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread(); }
            ExcusiveMode.OnNext(_isExcusiveMode);
        }                               
    }
}