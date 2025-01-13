using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace FVH.SSHF.Infrastructure.Input
{
    internal class InitializationCondition
    {
        private bool _isComplete = false;
        private readonly BehaviorSubject<bool> _initializationCompleteBehaviorSubject;

        internal readonly IObservable<bool> _initializationComplete;
        internal InitializationCondition()
        {
            _initializationCompleteBehaviorSubject = new BehaviorSubject<bool>(false);
            _initializationComplete = _initializationCompleteBehaviorSubject.AsObservable();
        }
        internal void InitializationComplete()
        {
            if(_isComplete is true) throw new InvalidOperationException("Reinitialization is not possible");

            _isComplete = true;

            _initializationCompleteBehaviorSubject.OnNext(_isComplete);
            _initializationCompleteBehaviorSubject.OnCompleted();
            _initializationCompleteBehaviorSubject.Dispose();
        }
    }
}
