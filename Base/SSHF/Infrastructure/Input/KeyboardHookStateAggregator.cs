using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

using R3;


namespace FVH.SSHF.Infrastructure.Input
{
    internal sealed class KeyboardHookStateAggregator : IDisposable
    {
        private readonly BehaviorSubject<bool> _isAppInitialized;
        private readonly BehaviorSubject<bool> _isNotInExclusiveMode;
        private readonly IDisposable _exclusiveModeSubscription;

        public ReadOnlyReactiveProperty<bool> HookCanBeActive { get; }

        public KeyboardHookStateAggregator(Observable<bool> isInExclusiveModeSource)
        {
            _isAppInitialized     = new BehaviorSubject<bool>(false); 
            _isNotInExclusiveMode = new BehaviorSubject<bool>(true);

            Observable<bool> notInExclusiveModeStream = isInExclusiveModeSource.Select(isInExclusiveMode => isInExclusiveMode is false);

            _exclusiveModeSubscription = notInExclusiveModeStream.Subscribe
            (
                NextValue => _isNotInExclusiveMode.OnNext(NextValue),
                ex        => _isNotInExclusiveMode.OnCompleted(Result.Failure(ex)),
                result    => _isNotInExclusiveMode.OnCompleted(result)
            );

            HookCanBeActive = _isAppInitialized.CombineLatest(_isNotInExclusiveMode, (initialized, notExclusive) => initialized && notExclusive).ToReadOnlyReactiveProperty(initialValue: false); 
        }
        public void NotifyAppInitialized()
        {
            _isAppInitialized.OnNext(true);
            _isAppInitialized.OnCompleted();
        }
        public void Dispose()
        {
            _exclusiveModeSubscription.Dispose();
            _isAppInitialized.Dispose();
            _isNotInExclusiveMode.Dispose();
            HookCanBeActive.Dispose(); 
        }
    }
}