using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure.Input;
using System.Threading;
using System.Diagnostics;
using R3;

namespace FVH.SSHF.FastWindowArea
{
    internal struct ShortcutsFunction
    {
        internal string NameFunction;
        internal VKeys[] Shortcut;
    }
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
        private readonly BehaviorSubject<IKeyboardHandler?> _keyboardHandler;
        private readonly WaitingInputProvider _waitingInputProvider;

        internal bool IsInitialize = false;
        internal bool BlockInput = false;
        internal FastWindowManager
        (
            Dispatcher dispatcher,
            Func<FastWindowViewModelDependencies> getFastWindowViewModelDependencies,
            BehaviorSubject<IKeyboardHandler?> keyboardHandler,
            WaitingInputProvider waitingInputProvider
        )
        {
            _dispatcher = dispatcher;
            _fastWindows = new Dictionary<int, OneFastWindow>();
            _keyboardHandler = keyboardHandler;
            _windowCreator = new FastWindowCreator(dispatcher, getFastWindowViewModelDependencies);

            _currentStatusShortcutsFastWindow = new BehaviorSubject<IEnumerable<KeyboardShortcut>>(GetDefaultShortcuts());

            _waitingInputProvider = waitingInputProvider;

            _waitingInputProvider.IsDisposeInput.ObserveOnThreadPool().Subscribe(onNext: IfInputDispose);
        }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            IsDisposed = true;
            Array.ForEach(_fastWindows.Select(value => value.Value).ToArray(), oneFastWindow => oneFastWindow.Dispose());
            _fastWindows.Clear();
        }
        private void IfInputDispose(bool disposeInput)
        {
            if(disposeInput is false) return;
            if(_activeFastWindow?.FastWindowViewModelDependencies?.IWindowPositionUpdater?.IsUpdateWindow is true) _activeFastWindow?.FastWindowCommand.StopRefreshWindow().Wait();

            if(_activeFastWindow?.FastWindowCommand.MainWindowViewModel.VisibleCondition.Value == System.Windows.Visibility.Visible) _activeFastWindow?.FastWindowCommand.HideWindow().Wait();
        }
        internal void SetNewShortcuts(ShortcutsFunction[] shortcutsFunction)
        {
            void SetNewShortcut(ShortcutsFunction shortcutFunction)
            {
                KeyboardShortcut[] arrayShortcuts = _currentShortcutsFastWindow;
                KeyboardShortcut? singleElement = arrayShortcuts.Single((shortcut) =>
                {
                    ArgumentNullException.ThrowIfNull(shortcut.Identifier);
                    return shortcut.Identifier.ToString() == shortcutFunction.NameFunction;
                });
                singleElement.KeyCombo.Value = shortcutFunction.Shortcut;
            }

            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(IsInitialize is not true) throw new InvalidOperationException("IsInitialize is false");
            if(_currentShortcutsFastWindow is null) throw new NullReferenceException("_currentShortcutsFastWindow is null");

            Array.ForEach(shortcutsFunction, (shortcut) =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(shortcut.NameFunction);
                ArgumentNullException.ThrowIfNull(shortcut.Shortcut);
            });
            Array.ForEach(shortcutsFunction, SetNewShortcut);

            _currentStatusShortcutsFastWindow.OnNext(_currentShortcutsFastWindow);
        }
        internal async Task CreateMainWindow()
        {
            if(IsInitialize is true) throw new InvalidOperationException("Object already initialized created");
            IsInitialize = true;

            OneFastWindow firstFastWindow = await CreateFastWindowAsync();

            _firstFastWindow = firstFastWindow;
            _activeFastWindow = firstFastWindow;

            _currentShortcutsFastWindow = GetDefaultShortcuts();
        }
        public BehaviorSubject<IEnumerable<KeyboardShortcut>> GetShortcutsAsObservable() => _currentStatusShortcutsFastWindow;

        public IEnumerable<KeyboardShortcut> GetShortcuts()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(IsInitialize is false) throw new InvalidOperationException("The object must be initialized");
            ArgumentNullException.ThrowIfNull(_activeFastWindow);
            return _currentShortcutsFastWindow!;
        }
        private Task DisposeActiveFastWindowAsync()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(_firstFastWindow!.Equals(_activeFastWindow) is true) return Task.CompletedTask;
            _activeFastWindow!.Dispose();
            _fastWindows.Remove(_currentIndexFastWindow);
            _currentIndexFastWindow--;
            return Task.CompletedTask;
        }
        private async Task<OneFastWindow> CreateFastWindowAsync()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> task = _dispatcher.Invoke(_windowCreator.CreateFastWindowAsync);

            (FastWindow FastWindow, FastWindowViewModel FastWindowViewModel, FastWindowViewModelDependencies FastWindowViewModelDependencies) fastWindow = await task;

            //(FastWindow FastWindow, FastWindowViewModel FastWindowViewModel, FastWindowViewModelDependencies FastWindowViewModelDependencies) fastWindow =
            //await await _dispatcher.InvokeAsync(_windowCreator.CreateFastWindowAsync).Task;

            FastWindowExternalConditions fastWindowExternalConditions = new FastWindowExternalConditions(fastWindow.FastWindowViewModel, _keyboardHandler);
            FastWindowCommand fastWindowCommand = new FastWindowCommand(fastWindow.FastWindow, fastWindow.FastWindowViewModel);
            OneFastWindow oneFastWindow = new OneFastWindow(fastWindow.FastWindow, fastWindow.FastWindowViewModelDependencies, fastWindowExternalConditions, fastWindowCommand);

            _fastWindows[_fastWindows.Count + 1] = oneFastWindow;
            _currentIndexFastWindow++;

            _dispatcher.Invoke(() => fastWindow.FastWindow.Name = $"Fast_index_{_currentIndexFastWindow}");

            return oneFastWindow;
        }
        internal void SetNewShortcuts(KeyboardShortcut[] shortcuts) => _currentShortcutsFastWindow = shortcuts;
        internal KeyboardShortcut[] GetDefaultShortcuts() =>
        [
            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            ],
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.PresentNewImage(), nameof(_activeFastWindow.FastWindowCommand.PresentNewImage)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
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
                VKeys.VK_CONTROL
            ],
            () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.StopRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.StopRefreshWindow)),

            new KeyboardShortcut(
            [
                VKeys.VK_SCROLL
            ],
            () => _activeFastWindow!.FastWindowCommand.InvokeMsScreenClip(), $"SCROLL_{nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)}"),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_ADD
            ],
            () => BlockInput is true ? Task.CompletedTask : CreateWindowAsync(), nameof(CreateWindowAsync)),

            new KeyboardShortcut(
            [
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_SUBTRACT
            ],
            () => BlockInput is true ? Task.CompletedTask : DisposeActiveWindowAsync(), nameof(DisposeActiveWindowAsync))
        ];
        internal async Task CreateWindowAsync()
        {
            if(BlockInput is true) return;
            BlockInput = true;
            OneFastWindow fastWindow = await await _dispatcher.InvokeAsync(CreateFastWindowAsync);
            _activeFastWindow = fastWindow;
            BlockInput = false;
        }
        internal async Task DisposeActiveWindowAsync()
        {
            BlockInput = true;
            await await _dispatcher.InvokeAsync(DisposeActiveFastWindowAsync);
            _activeFastWindow = _fastWindows[_currentIndexFastWindow];
            BlockInput = false;
        }

    }
}

