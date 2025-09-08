using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

using R3;


namespace FVH.SSHF.Infrastructure.Input
{
    internal class AggregatorInputConditions : IDisposable
    {
        private bool _isDisposed = false;
        private readonly Dictionary<R3.Observable<bool>, (bool? CurretStatus, IDisposable ObservableDispose)> _currentObservableConditions;
        internal readonly R3.BehaviorSubject<bool> InputConditionsBehaviorSubject;
        internal AggregatorInputConditions()
        {
            _currentObservableConditions = new Dictionary<R3.Observable<bool>, (bool?, IDisposable)>();
            InputConditionsBehaviorSubject = new R3.BehaviorSubject<bool>(false);
        }
        private void OnNextCondition(R3.Observable<bool> currentObservable, bool nextCondition)
        {
            bool? previousState = _currentObservableConditions[currentObservable].CurretStatus;

            _currentObservableConditions[currentObservable] = (nextCondition, _currentObservableConditions[currentObservable].ObservableDispose);

            if(nextCondition is true)
            {
                if(InputConditionsBehaviorSubject.Value is false) InputConditionsBehaviorSubject.OnNext(true);
                return;
            }

            if(previousState is true)
            {
                bool anyTrue = _currentObservableConditions.Any(item => item.Value.CurretStatus is true);
                if(InputConditionsBehaviorSubject.Value != anyTrue) 
                {
                    InputConditionsBehaviorSubject.OnNext(anyTrue);

                }
            }
        }
        internal void AddIObservable(R3.Observable<bool> conditionObservable)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _currentObservableConditions.Add(conditionObservable, (null, R3.Disposable.Empty));

            TaskCompletionSource tcs = new TaskCompletionSource();
            bool lastValue = false;
            IDisposable subscription = conditionObservable.SubscribeOnThreadPool().ObserveOnThreadPool().Subscribe(onNext: (bool condition) =>
              {
                  lastValue = condition;
                  OnNextCondition(conditionObservable, condition);
                  _ = tcs.TrySetResult();
              }, onCompleted: (Result r) =>
              {
                  Release(conditionObservable);
                  _ = tcs.TrySetResult();
              });
            tcs.Task.Wait();

            _currentObservableConditions[conditionObservable] = (lastValue, subscription);
        }
        private void Release(R3.Observable<bool> observable, Exception? _ = null)
        {
            _currentObservableConditions[observable].ObservableDispose.Dispose();
            _currentObservableConditions.Remove(observable);
        }
        public void Dispose()
        {
            if(_isDisposed is true) return;
            foreach((bool?, IDisposable) value in _currentObservableConditions.Values) { value.Item2.Dispose(); }

            _currentObservableConditions.Clear();
            InputConditionsBehaviorSubject.Dispose();
            _isDisposed = true;
        }
    }
}
