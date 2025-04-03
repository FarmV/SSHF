using System;



namespace FVH.SSHF.Infrastructure.Input
{
    internal class InitializationCondition
    {
        private bool _isComplete = false;
        private readonly R3.BehaviorSubject<bool> InitializationsCompleteBehaviorSubject;

        internal InitializationCondition()
        {
            InitializationsCompleteBehaviorSubject = new R3.BehaviorSubject<bool>(false);
        }
        internal void InitializationComplete()
        {
            if(_isComplete is true) throw new InvalidOperationException("Reinitialization is not possible");

            _isComplete = true;

            InitializationsCompleteBehaviorSubject.OnNext(_isComplete);
            InitializationsCompleteBehaviorSubject.OnCompleted(R3.Result.Success);
            InitializationsCompleteBehaviorSubject.Dispose();
        }
    }
}
