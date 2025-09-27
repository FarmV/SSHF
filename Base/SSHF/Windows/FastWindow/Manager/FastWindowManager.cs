using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using ABI.System;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Win32;

using R3;

namespace FVH.SSHF.FastWindowArea
{
    internal partial class FastWindowManager : IBehaviorSubjectGlobalShortcuts, IDisposable
    {
        private const int DelayHideWindowForMsScreenClip = 750;
        private int    _currentIndexFastWindow = 0;
        private bool    _isDisposed = false;
        private readonly R3.CompositeDisposable _disposables;
        private readonly Dispatcher _dispatcher;
        private readonly FastWindowCreator _windowCreator;
        private readonly Dictionary<int, OneFastWindow> _fastWindows;
        private readonly Lock _lockObjFastWindowsDictionary;
        private readonly BehaviorSubject<IEnumerable<KeyboardShortcut>> _currentStatusShortcutsFastWindow;
        private readonly WaitingInputProvider _waitingInputProvider;
        private readonly ObserverMsScreenClipExecuting _observerMsScreenClipExecuting;
        private OneFastWindow? _firstFastWindow;
        private OneFastWindow? _activeFastWindow;
        private KeyboardShortcut[]? _currentShortcutsFastWindow;

        internal bool IsInitialize = false;
        internal bool BlockInput = false;
        internal FastWindowManager
        (
            Dispatcher dispatcher,
            Func<FastWindowViewModelDependencies> getFastWindowViewModelDependencies,
            WaitingInputProvider waitingInputProvider,
            Observable<bool> isInExclusiveModeSource,
            ObserverMsScreenClipExecuting observerMsScreenClipExecuting
        )
        {
            _lockObjFastWindowsDictionary = new Lock();
            _dispatcher = dispatcher;
            _fastWindows = new Dictionary<int, OneFastWindow>();
            _windowCreator = new FastWindowCreator(dispatcher, getFastWindowViewModelDependencies);

            _disposables = new CompositeDisposable();

            _observerMsScreenClipExecuting = observerMsScreenClipExecuting;
            _disposables.Add(observerMsScreenClipExecuting.IsExecutingProcessScreenClip.ObserveOnThreadPool().SubscribeAwait(async (bool isExecuting,CancellationToken _) =>
            {
                if(_activeFastWindow is null) return;
                if(_activeFastWindow!.FastWindowCommand.IsExecutePresentNewImages is true && isExecuting is true)
                {
                    System.TimeSpan empiricalTimeoutSpinWait = System.TimeSpan.FromMilliseconds(32);

                    bool r = SpinWait.SpinUntil(() =>
                    {
                        return MsScreenClip.IsEnableProcessHost() is false;

                    }, empiricalTimeoutSpinWait);

                    if(r is true) return;

                }
                if(isExecuting is true) await HideAllWindow(DelayHideWindowForMsScreenClip);
                else { await ShowAllWindowExcludingActiveWindow(); }
            },awaitOperation: AwaitOperation.ThrottleFirstLast, configureAwait: false));

            _currentStatusShortcutsFastWindow = new BehaviorSubject<IEnumerable<KeyboardShortcut>>(GetDefaultShortcuts());

            _waitingInputProvider = waitingInputProvider;

            _disposables.Add(isInExclusiveModeSource.ObserveOnThreadPool().
             SubscribeAwait(onNextAsync: async (bool next, CancellationToken token) => await IfExclusiveMode(next, token), AwaitOperation.Switch));
        }
        public void Dispose()
        {
            if(_isDisposed is true) return;
            _isDisposed = true;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                Array.ForEach(_fastWindows.Select(value => value.Value).ToArray(), oneFastWindow => oneFastWindow.Dispose());
            }
            _fastWindows.Clear();
            _disposables.Dispose();
        }
        public BehaviorSubject<IEnumerable<KeyboardShortcut>> GetShortcutsAsObservable() => _currentStatusShortcutsFastWindow;
        public IEnumerable<KeyboardShortcut> GetShortcuts()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            if(IsInitialize is false) throw new InvalidOperationException("The object must be initialized");
            ArgumentNullException.ThrowIfNull(_activeFastWindow);
            return _currentShortcutsFastWindow!;
        }
        internal async Task<FastWindow> CreateMainWindow()
        {
            if(IsInitialize is true) throw new InvalidOperationException("Object already initialized created");
            IsInitialize = true;

            OneFastWindow firstFastWindow = await CreateFastWindowAsync();
            _ =_dispatcher.Invoke(() => _ = firstFastWindow.FastWindow.Tag = nameof(_firstFastWindow));


            _dispatcher.Invoke(() =>
            {
                using(_lockObjFastWindowsDictionary.EnterScope())
                {
                    if(_fastWindows[1].FastWindow.Tag is not nameof(_firstFastWindow)) throw new InvalidOperationException();
                }
            });


            _firstFastWindow = firstFastWindow;
            _activeFastWindow = firstFastWindow;

            _currentShortcutsFastWindow = GetDefaultShortcuts();
            return firstFastWindow.FastWindow;
        }
        internal void SetNewShortcuts(KeyboardShortcut[] shortcuts) => _currentShortcutsFastWindow = shortcuts;
        internal KeyboardShortcut[] GetDefaultShortcuts() =>
        [
            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_LSHIFT,
                VKeys.VK_KEY_A
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : _activeFastWindow!.FastWindowCommand.PresentNewImage(), nameof(_activeFastWindow.FastWindowCommand.PresentNewImage),() => _activeFastWindow!.FastWindowCommand.IsExecutePresentNewImages is false),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_LSHIFT,
                VKeys.VK_KEY_S
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : _activeFastWindow!.FastWindowCommand.InvokeMsScreenClip(), nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)),

            new KeyboardShortcut(
            [
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : _activeFastWindow!.FastWindowCommand.SwitchBlockRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.SwitchBlockRefreshWindow)),

            new KeyboardShortcut(
            [
                VKeys.VK_LCONTROL
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : _activeFastWindow!.FastWindowCommand.StopRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.StopRefreshWindow),() => 
            {
                if(BlockInput is true) return false;
                return _activeFastWindow!.FastWindowCommand.CanExecuteStopRefreshWindow();
            }),

            new KeyboardShortcut(
            [
                VKeys.VK_SCROLL
            ],
            () => _activeFastWindow!.FastWindowCommand.InvokeMsScreenClip(), $"SCROLL_{nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)}"),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_LSHIFT,
                VKeys.VK_ADD
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : new ValueTask(CreateWindowAsync()), nameof(CreateWindowAsync)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SCROLL,
            ],
            () => BlockInput is true ? ValueTask.CompletedTask : new ValueTask(HideAllWindowAsScreenClip().ContinueWith(async (Task t) => 
            {
                await t;
                await Task.Delay(200);
                await DisposeAllWindowExcludingFirsWindow();
            })), nameof(HideAllWindowAsScreenClip))
        ];
        private Task DisposeAllWindowExcludingFirsWindow()
        {
            _activeFastWindow = _firstFastWindow;

            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                _ = _fastWindows.Remove(1);

                _dispatcher.Invoke(() => { foreach(OneFastWindow window in _fastWindows.Values) window.Dispose(); });
                _fastWindows.Clear();

                _fastWindows[1] = _firstFastWindow!;
            }

            return Task.CompletedTask;
        }
        private async ValueTask HideAllWindow(int delayHide = 0)
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            ParallelQuery<Task> taskWindowList;
            using(_lockObjFastWindowsDictionary.EnterScope()) 
            { 
                taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async (OneFastWindow one) =>
                {
                    await Task.Delay(delayHide);
                    if(one?.FastWindowCommand?.MainWindowViewModel?.PositionManager.IsUpdateWindow is true) await one?.FastWindowCommand?.MainWindowViewModel?.StopUpdateWindow()!;
                    if(one?.FastWindowCommand?.MainWindowViewModel?.VisibleCondition?.CurrentValue == System.Windows.Visibility.Visible) one?.FastWindowCommand?.MainWindowViewModel?.HideWindow();

                });
            }
            await Task.WhenAll(taskWindowList);              
                    
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task ShowAllWindowExcludingActiveWindow()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();
            
            ParallelQuery<ValueTask> taskWindowList;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select((OneFastWindow one) =>
                {
                     if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Hidden)
                     {
                         if(one == _activeFastWindow) return ValueTask.CompletedTask;
                         one.FastWindowCommand.MainWindowViewModel.ShowWindow();
                     }
                     return ValueTask.CompletedTask;
                });
            }
            await Task.WhenAll(taskWindowList.Select(vt => vt.AsTask()));
           
          
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task HideAllWindowAsScreenClip()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            MsScreenClip.Invoke();

            ParallelQuery<Task> taskWindowList;

            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async (OneFastWindow one) =>
                {
                    if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible)
                    {
                        await Task.Delay(DelayHideWindowForMsScreenClip);
                        await one.FastWindowCommand.HideWindow();
                    }
                });
            }
            await Task.WhenAll(taskWindowList.ToArray());


            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async ValueTask IfExclusiveMode(bool isExclusiveMode, CancellationToken token)
        {
            if(isExclusiveMode is false) return;

            if(token.IsCancellationRequested is true) return;

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            List<FastWindowViewModel> listViewModels;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                listViewModels = _fastWindows.Values.Select(x => x.FastWindowCommand.MainWindowViewModel).ToList();
            }

            foreach(FastWindowViewModel viewMode in listViewModels) 
            {
                if(viewMode.PositionManager.IsUpdateWindow is true) await viewMode.StopUpdateWindow();
                if(viewMode.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible) viewMode.HideWindow();
            }
        
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();          
        }
        private async Task CreateWindowAsync()
        {
            if(BlockInput is true) return;
            BlockInput = true;
            OneFastWindow fastWindow = await _dispatcher.InvokeAsync(CreateFastWindowAsync).Task.Unwrap();
            _activeFastWindow = fastWindow;
            BlockInput = false;
        }      
        private async Task<OneFastWindow> CreateFastWindowAsync()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> task = _dispatcher.Invoke(_windowCreator.CreateFastWindowAsync);

            (FastWindow FastWindow, FastWindowViewModel FastWindowViewModel, FastWindowViewModelDependencies FastWindowViewModelDependencies) fastWindow = await task;

            FastWindowExternalConditions fastWindowExternalConditions = new FastWindowExternalConditions(fastWindow.FastWindowViewModel, _waitingInputProvider);
            FastWindowCommand fastWindowCommand = new FastWindowCommand(fastWindow.FastWindow, fastWindow.FastWindowViewModel);
            OneFastWindow oneFastWindow = new OneFastWindow(fastWindow.FastWindow, fastWindow.FastWindowViewModelDependencies, fastWindowExternalConditions, fastWindowCommand);


            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                _fastWindows[_fastWindows.Count + 1] = oneFastWindow;
            }

            _ = Interlocked.Increment(ref _currentIndexFastWindow);
            _currentIndexFastWindow++;

            _ = _dispatcher.Invoke(() => fastWindow.FastWindow.Name = $"Fast_index_{_currentIndexFastWindow}");

            return oneFastWindow;
        }
    }
}