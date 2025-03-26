using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using R3;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Win32;

namespace FVH.SSHF.FastWindowArea
{
    internal partial class FastWindowManager : IBehaviorSubjectGlobalShortcuts, IDisposable
    {
        private int _currentIndexFastWindow = 0;
        private bool IsDisposed = false;
        private readonly Dispatcher _dispatcher;
        private readonly FastWindowCreator _windowCreator;
        private readonly Dictionary<int, OneFastWindow> _fastWindows;
        private readonly BehaviorSubject<IEnumerable<KeyboardShortcut>> _currentStatusShortcutsFastWindow;
        private OneFastWindow? _firstFastWindow;
        private OneFastWindow? _activeFastWindow;
        private KeyboardShortcut[]? _currentShortcutsFastWindow;
        private readonly WaitingInputProvider _waitingInputProvider;
        private readonly ObserverMsScreenClipExecuting _observerMsScreenClipExecuting;

        internal bool IsInitialize = false;
        internal bool BlockInput = false;
        internal FastWindowManager
        (
            Dispatcher dispatcher,
            Func<FastWindowViewModelDependencies> getFastWindowViewModelDependencies,
            WaitingInputProvider waitingInputProvider,
            ObserverMsScreenClipExecuting observerMsScreenClipExecuting
        )
        {
            _dispatcher = dispatcher;
            _fastWindows = new Dictionary<int, OneFastWindow>();
            _windowCreator = new FastWindowCreator(dispatcher, getFastWindowViewModelDependencies);

            _observerMsScreenClipExecuting = observerMsScreenClipExecuting;
            _ = observerMsScreenClipExecuting.IsExecutingProccesScreenClip.ObserveOnThreadPool().SubscribeAwait(async (bool isExecuting,CancellationToken _) =>
              {
                  if(isExecuting is true) await HideAllWindow2(200);
                  else { await ShowAllWindowExcludingActiveWindow(); }
              },awaitOperation: AwaitOperation.ThrottleFirstLast,configureAwait:false);

            _currentStatusShortcutsFastWindow = new BehaviorSubject<IEnumerable<KeyboardShortcut>>(GetDefaultShortcuts());

            _waitingInputProvider = waitingInputProvider;

            _ = _waitingInputProvider.CurrentStatusSubscribeInput.ObserveOnThreadPool().Skip(1).
              SubscribeAwait(onNextAsync: async (bool next, CancellationToken _) => await IfInputDispose(next), AwaitOperation.ThrottleFirstLast);
        }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            IsDisposed = true;
            Array.ForEach(_fastWindows.Select(value => value.Value).ToArray(), oneFastWindow => oneFastWindow.Dispose());
            _fastWindows.Clear();
        }
        public BehaviorSubject<IEnumerable<KeyboardShortcut>> GetShortcutsAsObservable() => _currentStatusShortcutsFastWindow;
        public IEnumerable<KeyboardShortcut> GetShortcuts()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(IsInitialize is false) throw new InvalidOperationException("The object must be initialized");
            ArgumentNullException.ThrowIfNull(_activeFastWindow);
            return _currentShortcutsFastWindow!;
        }
        internal async Task CreateMainWindow()
        {
            if(IsInitialize is true) throw new InvalidOperationException("Object already initialized created");
            IsInitialize = true;

            OneFastWindow firstFastWindow = await CreateFastWindowAsync();
            _ =_dispatcher.Invoke(() => _ = firstFastWindow.FastWindow.Tag = nameof(_firstFastWindow));
            _dispatcher.Invoke(() => { if(_fastWindows[1].FastWindow.Tag is not nameof(_firstFastWindow)) throw new InvalidOperationException(); });

            _firstFastWindow = firstFastWindow;
            _activeFastWindow = firstFastWindow;

            _currentShortcutsFastWindow = GetDefaultShortcuts();
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
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.PresentNewImage(), nameof(_activeFastWindow.FastWindowCommand.PresentNewImage)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_LSHIFT,
                VKeys.VK_KEY_S
            ],
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.InvokeMsScreenClip(), nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)),

            new KeyboardShortcut(
            [
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            ],
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.SwitchBlockRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.SwitchBlockRefreshWindow)),

            new KeyboardShortcut(
            [
                VKeys.VK_LCONTROL
            ],
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.StopRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.StopRefreshWindow),() => 
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
            () => BlockInput is true ? Task.CompletedTask : CreateWindowAsync(), nameof(CreateWindowAsync)),

            //new KeyboardShortcut(
            //[
            //    VKeys.VK_LWIN,
            //    VKeys.VK_LSHIFT,
            //    VKeys.VK_SUBTRACT
            //],
            //() => BlockInput is true ? Task.CompletedTask : DisposeActiveWindowAsync(), nameof(DisposeActiveWindowAsync)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SCROLL,
            ],
            () => BlockInput is true ? Task.CompletedTask : HideAllWindowAsScreenClip().ContinueWith((Task _) => DisposeAllWindowExcludingFirsWindow()), nameof(HideAllWindowAsScreenClip))
        ];
  
        private Task DisposeAllWindowExcludingFirsWindow()
        {
            _activeFastWindow = _firstFastWindow;

            _ = _fastWindows.Remove(1);

            _dispatcher.Invoke(() => { foreach(OneFastWindow window in _fastWindows.Values) window.Dispose(); });
            _fastWindows.Clear();

            _fastWindows[1] = _firstFastWindow!;

            return Task.CompletedTask;
        }
        private async Task HideAllWindow2(int delayHide = 0)
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            ParallelQuery<Task> taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async (OneFastWindow one) =>
            {
                if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible)
                {
                    if(delayHide is not 0) await Task.Delay(delayHide);
                    await one.FastWindowCommand.HideWindow();
                }              
            });
            await Task.WhenAll(taskWindowList.ToArray());

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task ShowAllWindowExcludingActiveWindow()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            ParallelQuery<Task> taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async (OneFastWindow one) =>
            {
                if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Hidden)
                {
                    if(one == _activeFastWindow ) return;
                    await one.FastWindowCommand.ShowWindow();
                }
            });
            await Task.WhenAll(taskWindowList.ToArray());

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task HideAllWindowAsScreenClip()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            MsScreenClip.Invoke();
            ParallelQuery<Task> taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async (OneFastWindow one) =>
            { 
                if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible)
                {
                    await Task.Delay(200);
                    await one.FastWindowCommand.HideWindow();
                }                          
            });
            await Task.WhenAll(taskWindowList.ToArray());

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task IfInputDispose(bool statusSubscribeInput)
        {
            if(statusSubscribeInput is true) return;

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();
                        
            ParallelQuery<Task> taskWindowList = _fastWindows.Values.AsParallel().AsUnordered().Select(async one =>
            {
                Task taskStopRefreshWindow = one.FastWindowCommand.MainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is true
                      ? one.FastWindowCommand.StopRefreshWindow()
                       : Task.CompletedTask; 

                Task taskHideWindow = one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible
                      ? one.FastWindowCommand.HideWindow()
                       : Task.CompletedTask; 

                await taskStopRefreshWindow;
                await taskHideWindow;
            });
            await Task.WhenAll(taskWindowList.ToArray());

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
        private async Task DisposeActiveWindowAsync()
        {
            BlockInput = true;
            await _dispatcher.InvokeAsync(DisposeActiveFastWindowAsync).Task.Unwrap();
            _activeFastWindow = _fastWindows[_currentIndexFastWindow];
            BlockInput = false;
        }
        private Task DisposeActiveFastWindowAsync()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(_firstFastWindow!.Equals(_activeFastWindow) is true) return Task.CompletedTask;
            _activeFastWindow!.Dispose();
            _ = _fastWindows.Remove(_currentIndexFastWindow);
            _currentIndexFastWindow--;
            return Task.CompletedTask;
        }
        private async Task<OneFastWindow> CreateFastWindowAsync()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> task = _dispatcher.Invoke(_windowCreator.CreateFastWindowAsync);

            (FastWindow FastWindow, FastWindowViewModel FastWindowViewModel, FastWindowViewModelDependencies FastWindowViewModelDependencies) fastWindow = await task;

            FastWindowExternalConditions fastWindowExternalConditions = new FastWindowExternalConditions(fastWindow.FastWindowViewModel, _waitingInputProvider);
            FastWindowCommand fastWindowCommand = new FastWindowCommand(fastWindow.FastWindow, fastWindow.FastWindowViewModel);
            OneFastWindow oneFastWindow = new OneFastWindow(fastWindow.FastWindow, fastWindow.FastWindowViewModelDependencies, fastWindowExternalConditions, fastWindowCommand);

            _fastWindows[_fastWindows.Count + 1] = oneFastWindow;
            _currentIndexFastWindow++;

            _ = _dispatcher.Invoke(() => fastWindow.FastWindow.Name = $"Fast_index_{_currentIndexFastWindow}");

            return oneFastWindow;
        }
    }
}