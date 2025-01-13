using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace FVH.SSHF.Infrastructure.Input
{
    internal class AggregatorInputConditions : IDisposable
    {
        private bool _isDisposed = false;
        private readonly Dictionary<IObservable<bool>, (bool? CurretStatus, IDisposable ObservableDispose)> _currentObservableConditions;
        internal readonly BehaviorSubject<bool> InputConditionsBehaviorSubject;
        internal AggregatorInputConditions()
        {
            _currentObservableConditions = new Dictionary<IObservable<bool>, (bool?, IDisposable)>();
            InputConditionsBehaviorSubject = new BehaviorSubject<bool>(false);
        }
        private void OnNextCondition(IObservable<bool> currentObservable, bool nextCondition)
        {
            _currentObservableConditions[currentObservable] = (nextCondition, _currentObservableConditions[currentObservable].ObservableDispose);

            if(nextCondition is true)
            {
                if(InputConditionsBehaviorSubject.Value is false) InputConditionsBehaviorSubject.OnNext(true);
                return;
            }
            bool anyTrue = _currentObservableConditions.Any(item => item.Value.CurretStatus is true);
            if(InputConditionsBehaviorSubject.Value != anyTrue) InputConditionsBehaviorSubject.OnNext(anyTrue);
        }
        internal void AddIObservable(IObservable<bool> conditionObservable)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _currentObservableConditions.Add(conditionObservable, (null, Disposable.Empty));

            IDisposable subscription = conditionObservable.ObserveOn(System.Reactive.Concurrency.TaskPoolScheduler.Default).Subscribe
            (
                onNext: (condition) => OnNextCondition(conditionObservable, condition),
                onError: (ex) => Release(conditionObservable, ex),
                onCompleted: () => Release(conditionObservable)
            );

            _currentObservableConditions[conditionObservable] = (null, subscription);
        }
        private void Release(IObservable<bool> observable, Exception? _ = null)
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
