using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Input;
using FVH.SSHF.Infrastructure.Interfaces;

using R3;

namespace FVH.SSHF.FastWindowArea
{
    internal sealed partial class FastWindowManager : IBehaviorSubjectGlobalShortcuts, IDisposable
    {
        private const int DelayHideWindowForMsScreenClip = 750;
        private int       _currentIndexFastWindow = 0;
        private bool      _isDisposed = false;
        private readonly  R3.CompositeDisposable _disposables;
        private readonly  Dispatcher _dispatcher;
        private readonly  FastWindowCreator _windowCreator;
        private readonly  Dictionary<int, OneFastWindow> _fastWindows;
        private readonly  Lock _lockObjFastWindowsDictionary;
        private readonly  BehaviorSubject<IEnumerable<KeyboardShortcut>> _currentStatusShortcutsFastWindow;
        private readonly  WaitingInputProvider _waitingInputProvider;
        private readonly  MsScreenClip _msScreenClip;
        private           OneFastWindow? _firstFastWindow;
        private           OneFastWindow? _activeFastWindow;
        private           KeyboardShortcut[]? _currentShortcutsFastWindow;

        private bool _isInitialize = false;
        private bool _blockInput   = false;
        public FastWindowManager
        (
            Dispatcher dispatcher,
            Func<FastWindowViewModelDependencies> getFastWindowViewModelDependencies,
            WaitingInputProvider waitingInputProvider,
            Observable<bool> isInExclusiveModeSource,
            MsScreenClip msScreenClip
        )
        {
            _lockObjFastWindowsDictionary = new Lock();
            _dispatcher = dispatcher;
            _fastWindows = new Dictionary<int, OneFastWindow>();
            _windowCreator = new FastWindowCreator(dispatcher, getFastWindowViewModelDependencies);

            _disposables = new CompositeDisposable();

            _msScreenClip = msScreenClip;

            _disposables.Add(_msScreenClip.IsClipping.ObserveOnThreadPool().SubscribeAwait(async (bool isExecuting, CancellationToken _) =>
            {
                if(Volatile.Read(ref _activeFastWindow) is null) return;
                if(isExecuting is true) await HideAllWindows(DelayHideWindowForMsScreenClip);
                else { await ShowAllWindowExcludingActiveWindow(); }
            }, awaitOperation: AwaitOperation.ThrottleFirstLast, configureAwait: false));

            _currentStatusShortcutsFastWindow = new BehaviorSubject<IEnumerable<KeyboardShortcut>>(GetDefaultShortcuts());

            _waitingInputProvider = waitingInputProvider;

            _disposables.Add(isInExclusiveModeSource.ObserveOnThreadPool().
             SubscribeAwait(onNextAsync: async (bool next, CancellationToken token) => await IfExclusiveMode(next, token), AwaitOperation.Switch));
        }
        public void Dispose()
        {
            if(Interlocked.CompareExchange(ref _isDisposed, true, false) is not false) return;

            OneFastWindow[] windowsToDispose;

            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToDispose = _fastWindows.Values.ToArray();
                _fastWindows.Clear();
            }
            Array.ForEach(windowsToDispose, oneFastWindow => oneFastWindow.Dispose());

            _disposables.Dispose();
        }
        public BehaviorSubject<IEnumerable<KeyboardShortcut>> GetShortcutsAsObservable() => _currentStatusShortcutsFastWindow;
        public IEnumerable<KeyboardShortcut> GetShortcuts()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);
            if(Volatile.Read(ref _isInitialize) is false) Throw(); [DoesNotReturn] static void Throw() => throw new InvalidOperationException("The object must be initialized");
            ArgumentNullException.ThrowIfNull(Volatile.Read(ref _activeFastWindow));
            return _currentShortcutsFastWindow!;
        }
        internal async Task<FastWindow> CreateMainWindowAsync()
        {
            if(Interlocked.CompareExchange(ref _isInitialize, true, false) is not false) Throw(); [DoesNotReturn] static void Throw() => throw new InvalidOperationException("Object already initialized.");

            OneFastWindow firstFastWindow;
            {
                Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> task = _dispatcher.Invoke(_windowCreator.CreateFastWindowAsync);
                (FastWindow FastWindow, FastWindowViewModel FastWindowViewModel, FastWindowViewModelDependencies FastWindowViewModelDependencies) fastWindowData = await task;

                FastWindowExternalConditions externalConditions = new FastWindowExternalConditions(fastWindowData.FastWindowViewModel, _waitingInputProvider);
                FastWindowCommand command = new(fastWindowData.FastWindow, fastWindowData.FastWindowViewModel);
                firstFastWindow = new OneFastWindow(fastWindowData.FastWindow, fastWindowData.FastWindowViewModelDependencies, externalConditions, command);
            }

            const int firstWindowKey = 1;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                _fastWindows[firstWindowKey] = firstFastWindow;
            }

            _currentIndexFastWindow = firstWindowKey;

            _dispatcher.Invoke(new Action(() =>
            {
                _ = firstFastWindow.FastWindow.Tag = nameof(_firstFastWindow);
                _ = firstFastWindow.FastWindow.Name = $"Fast_index_{firstWindowKey}";
            }));

            Volatile.Write(ref _firstFastWindow, firstFastWindow);
            Volatile.Write(ref _activeFastWindow, firstFastWindow);

            _currentShortcutsFastWindow = GetDefaultShortcuts();

            return firstFastWindow.FastWindow;
        }
        internal void SetNewShortcuts(KeyboardShortcut[] shortcuts) => _currentShortcutsFastWindow = shortcuts;
        internal KeyboardShortcut[] GetDefaultShortcuts() =>
           [
               new KeyboardShortcut([VKeys.VK_LWIN, VKeys.VK_LSHIFT, VKeys.VK_KEY_A ],
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return ValueTask.CompletedTask;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is null ? ValueTask.CompletedTask : activeWindow.FastWindowCommand.PresentNewImage();
               },
               nameof(FastWindowCommand.PresentNewImage),
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return false;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is not null && !activeWindow.FastWindowCommand.IsExecutePresentNewImages;
               }),

               new KeyboardShortcut( [ VKeys.VK_LWIN, VKeys.VK_LSHIFT, VKeys.VK_KEY_S ],
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return ValueTask.CompletedTask;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is null ? ValueTask.CompletedTask : activeWindow.FastWindowCommand.InvokeMsScreenClip();
               },
               nameof(FastWindowCommand.InvokeMsScreenClip)),

               new KeyboardShortcut([VKeys.VK_CONTROL, VKeys.VK_CAPITAL ],
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return ValueTask.CompletedTask;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is null ? ValueTask.CompletedTask : activeWindow.FastWindowCommand.SwitchBlockRefreshWindow();
               },
               nameof(FastWindowCommand.SwitchBlockRefreshWindow)),

               new KeyboardShortcut([ VKeys.VK_LCONTROL ],
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return ValueTask.CompletedTask;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is null ? ValueTask.CompletedTask : activeWindow.FastWindowCommand.StopRefreshWindow();
               },
               nameof(FastWindowCommand.StopRefreshWindow),
               () => // CanExecute
               {
                   if (Volatile.Read(ref _blockInput)) return false;
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is not null && activeWindow.FastWindowCommand.CanExecuteStopRefreshWindow();
               }),

               new KeyboardShortcut([ VKeys.VK_SCROLL ],
               () =>
               {
                   OneFastWindow? activeWindow = Volatile.Read(ref _activeFastWindow);
                   return activeWindow is null ? ValueTask.CompletedTask : activeWindow.FastWindowCommand.InvokeMsScreenClip();
               },
               $"SCROLL_{nameof(FastWindowCommand.InvokeMsScreenClip)}"),

               new KeyboardShortcut( [ VKeys.VK_LWIN, VKeys.VK_LSHIFT, VKeys.VK_ADD],
               () => Volatile.Read(ref _blockInput) ? ValueTask.CompletedTask : new ValueTask(CreateWindowAsync()),
               nameof(CreateWindowAsync)),

               new KeyboardShortcut([VKeys.VK_LWIN, VKeys.VK_SCROLL ],
               () =>
               {
                   if (Volatile.Read(ref _blockInput)) return ValueTask.CompletedTask;

                   async Task HideAndDisposeAsync()
                   {
                       await HideAllWindowAsScreenClip();
                       await Task.Delay(200);
                       await DisposeAllWindowExcludingFirsWindow();
                   }
                   return new ValueTask(HideAndDisposeAsync());
               },
               nameof(HideAllWindowAsScreenClip))
           ];
        private async Task DisposeAllWindowExcludingFirsWindow()
        {
            OneFastWindow[] windowsToDispose;
            OneFastWindow? firstWindow = Volatile.Read(ref _firstFastWindow);

            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToDispose = _fastWindows.Values.Where(w => w != firstWindow).ToArray();

                foreach(OneFastWindow window in windowsToDispose)
                {
                    KeyValuePair<int, OneFastWindow> item = _fastWindows.FirstOrDefault(kvp => kvp.Value == window);
                    if(item.Key is not 0) _ = _fastWindows.Remove(item.Key);
                }
            }

            void DisposeBatchAction()
            {
                foreach(OneFastWindow window in windowsToDispose)
                {
                    window.Dispose();
                }
            }

            await _dispatcher.InvokeAsync(DisposeBatchAction);

            Volatile.Write(ref _activeFastWindow, firstWindow);
        }
        private async ValueTask HideAllWindows(int delayHide = 0)
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            OneFastWindow[] windowsToProcess;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToProcess = _fastWindows.Values.ToArray();
            }

            if(delayHide > 0) await Task.Delay(delayHide);

            IEnumerable<Task> stopTasks = windowsToProcess
                             .Where(one => one.IsDisposed is false && one.FastWindowCommand.MainWindowViewModel.PositionManager.IsUpdateWindow is true)
                             .Select(one => one.FastWindowCommand.MainWindowViewModel.StopUpdateWindow());


            await Task.WhenAll(stopTasks);

            void HideBatchAction()
            {
                foreach(OneFastWindow one in windowsToProcess)
                {
                    if(one.IsDisposed is true) continue;

                    if(one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue is System.Windows.Visibility.Visible) one.FastWindowCommand.MainWindowViewModel.HideWindow();
                }
            }

            await _dispatcher.InvokeAsync(HideBatchAction);

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task ShowAllWindowExcludingActiveWindow()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();


            OneFastWindow[] windowsToProcess;
            OneFastWindow? activeWindow;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToProcess = _fastWindows.Values.ToArray();
            }
            activeWindow = Volatile.Read(ref _activeFastWindow);

            void ShowBatchAction()
            {
                foreach(OneFastWindow one in windowsToProcess)
                {
                    if(one.IsDisposed is false && one != activeWindow && one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue is System.Windows.Visibility.Hidden) one.FastWindowCommand.MainWindowViewModel.ShowWindow();

                }
            }

            await _dispatcher.InvokeAsync(ShowBatchAction);


            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task HideAllWindowAsScreenClip()
        {
            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            _msScreenClip.Invoke();

            OneFastWindow[] windowsToProcess;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToProcess = _fastWindows.Values.ToArray();
            }

            await Task.Delay(DelayHideWindowForMsScreenClip);

            void HideBatchAction()
            {
                foreach(OneFastWindow one in windowsToProcess)
                {
                    if(one.IsDisposed is false && one.FastWindowCommand.MainWindowViewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible)
                    {
                        one.FastWindowCommand.MainWindowViewModel.HideWindow();
                    }
                }
            }

            await _dispatcher.InvokeAsync(HideBatchAction);


            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async ValueTask IfExclusiveMode(bool isExclusiveMode, CancellationToken token)
        {
            if(isExclusiveMode is false || token.IsCancellationRequested is true) return;

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is false) _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            OneFastWindow[] windowsToProcess;
            using(_lockObjFastWindowsDictionary.EnterScope())
            {
                windowsToProcess = _fastWindows.Values.ToArray();
            }

            IEnumerable<Task> stopTasks = windowsToProcess
                             .Where(one => one.IsDisposed is false && one.FastWindowCommand.MainWindowViewModel.PositionManager.IsUpdateWindow is true)
                             .Select(one => one.FastWindowCommand.MainWindowViewModel.StopUpdateWindow());

            await Task.WhenAll(stopTasks);

            void HideBatchAction()
            {
                foreach(OneFastWindow one in windowsToProcess)
                {
                    if(one.IsDisposed is true) continue;
                    var viewModel = one.FastWindowCommand.MainWindowViewModel;
                    if(viewModel.VisibleCondition.CurrentValue == System.Windows.Visibility.Visible)
                    {
                        viewModel.HideWindow();
                    }
                }
            }

            await _dispatcher.InvokeAsync(HideBatchAction);

            if(SynchronizationContext.Current.InUIThreadTimeCriticalSection() is true) _ = SynchronizationContext.Current.StopSafeUITimeCriticalSection();
        }
        private async Task CreateWindowAsync()
        {
            if(Interlocked.CompareExchange(ref _blockInput, true, false) is not false) return;
            try
            {
                (FastWindow window, FastWindowViewModel vm, FastWindowViewModelDependencies deps) = await _windowCreator.CreateFastWindowAsync();

                FastWindowExternalConditions externalConditions = new(vm, _waitingInputProvider);
                FastWindowCommand command = new(window, vm);
                OneFastWindow oneFastWindow = new(window, deps, externalConditions, command);

                int newIndex = Interlocked.Increment(ref _currentIndexFastWindow);

                using(_lockObjFastWindowsDictionary.EnterScope())
                {
                    _fastWindows[newIndex] = oneFastWindow;
                }

                _ = _dispatcher.InvokeAsync(() => oneFastWindow.FastWindow.Name = $"Fast_index_{newIndex}");

                Volatile.Write(ref _activeFastWindow, oneFastWindow);
            }
            catch(Exception ex) { _ = Task.Run(() => ExceptionDispatchInfo.Capture(ex).Throw()); }
            finally
            {
                Volatile.Write(ref _blockInput, false);
            }
        }
    }
}