using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Linq;

using ReactiveUI;

using FVH.Background.Input.Infrastructure.Interfaces;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.FastWindowArea;
using FVH.SSHF.NotificationWindowArea;

namespace FVH.SSHF
{
    internal class FastWindowManager : IInvokeShortcuts, IDisposable
    {
        internal bool IsInitialize = false;
        internal bool BlockInput = false;
        private readonly Dispatcher _dispatcher;
        private readonly FastWindowCreator _windowCreator;
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly Dictionary<int, OneFastWindow> _fastWindows;
        private int _currentIndexFastWindow = 0;
        private bool IsDisposed = false;
        private OneFastWindow? _firstFastWindow;
        private OneFastWindow? _activeFastWindow;
        private Shortcuts[]? _shortcuts;

        internal FastWindowManager(Dispatcher dispatcher, Func<FastWindowViewModelDependencies> getFastWindowViewModelDependencies, IKeyboardHandler keyboardHandler)
        {
            _dispatcher = dispatcher;
            _fastWindows = new Dictionary<int, OneFastWindow>();
            _keyboardHandler = keyboardHandler;
            _windowCreator = new FastWindowCreator(dispatcher, getFastWindowViewModelDependencies);
        }
        internal void Initialize()
        {
            if(IsInitialize is true) throw new InvalidOperationException("The object is already initialized");
            IsInitialize = true;
            var task = CreateFastWindowAsync();

            OneFastWindow firstFastWindow = task.Result;

            _firstFastWindow = firstFastWindow;
            _activeFastWindow = firstFastWindow;

            _shortcuts = GetDefaultShortcuts();
        }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            IsDisposed = true;
            Array.ForEach(_fastWindows.Select(x => x.Value).ToArray(), x => x.Dispose());
            _fastWindows.Clear();
            GC.SuppressFinalize(this);
        }

        public IEnumerable<Shortcuts> GetShortcuts()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if(IsInitialize is false) throw new InvalidOperationException("The object must be initialized");
            ArgumentNullException.ThrowIfNull(_activeFastWindow);
            return _shortcuts!;
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
        internal void SetNewShortcuts(Shortcuts[] shortcuts) => _shortcuts = shortcuts;
        internal Shortcuts[] GetDefaultShortcuts() =>
        [
         new Shortcuts(
         [
             VKeys.VK_LWIN,
             VKeys.VK_SHIFT,
             VKeys.VK_KEY_A
         ],
         () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.PresentNewImage(), nameof(_activeFastWindow.FastWindowCommand.PresentNewImage)),

         new Shortcuts(
         [
             VKeys.VK_LWIN,
             VKeys.VK_SHIFT,
             VKeys.VK_KEY_S
         ],
         () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.InvokeMsScreenClip(), nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)),

         new Shortcuts(
         [
             VKeys.VK_CONTROL,
             VKeys.VK_CAPITAL
         ],
         () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.SwitchBlockRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.SwitchBlockRefreshWindow)),

         new Shortcuts(
         [
             VKeys.VK_CONTROL
         ],
         () => BlockInput is true ? Task.CompletedTask : _activeFastWindow!.FastWindowCommand.StopRefreshWindow(), nameof(_activeFastWindow.FastWindowCommand.StopRefreshWindow) ),

         new Shortcuts(
         [
             VKeys.VK_SCROLL
         ],
         new Func<Task>(_activeFastWindow!.FastWindowCommand.InvokeMsScreenClip),$"SCROLL_{nameof(_activeFastWindow.FastWindowCommand.InvokeMsScreenClip)}"),

         new Shortcuts(
         [
             VKeys.VK_LWIN,
             VKeys.VK_SHIFT,
             VKeys.VK_ADD
         ],
         new Func<Task>(() =>BlockInput is true ? Task.CompletedTask : CreateWindowAsync()), nameof(CreateWindowAsync)),
         new Shortcuts(
         [
             VKeys.VK_LWIN,
             VKeys.VK_SHIFT,
             VKeys.VK_SUBTRACT
         ],
         new Func<Task>(()=> BlockInput is true ? Task.CompletedTask : DisposeActiveWindowAsync()), nameof(DisposeActiveWindowAsync)),
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
    internal class OneFastWindow : IDisposable
    {
        internal bool IsDisposed = false;
        internal OneFastWindow(FastWindow fastWindow, FastWindowViewModelDependencies fastWindowViewModelDependencies, FastWindowExternalConditions mainWindowExternalConditions, FastWindowCommand fastWindowCommand)
        {
            FastWindow = fastWindow;
            FastWindowViewModelDependencies = fastWindowViewModelDependencies;
            MainWindowExternalConditions = mainWindowExternalConditions;
            FastWindowCommand = fastWindowCommand;
        }
        internal FastWindow FastWindow { get; init; }
        internal FastWindowViewModelDependencies FastWindowViewModelDependencies { get; init; }
        internal FastWindowExternalConditions MainWindowExternalConditions { get; init; }
        internal FastWindowCommand FastWindowCommand { get; init; }

        public void Dispose()
        {
            if(IsDisposed is true) return;
            IsDisposed = true;
            FastWindow.Close();
            FastWindowViewModelDependencies.Dispose();
            IsDisposed = true;
        }
    }
    internal class FastWindowCreator
    {
        private readonly Dispatcher _dispatcher;
        private readonly Func<FastWindowViewModelDependencies> _getFastWindowViewModelDependencies;
        internal FastWindowCreator(Dispatcher UiDispatcher, Func<FastWindowViewModelDependencies> fastWindowViewModelDependencies)
        {
            ArgumentNullException.ThrowIfNull(UiDispatcher);
            _dispatcher = UiDispatcher;
            _getFastWindowViewModelDependencies = fastWindowViewModelDependencies;
        }
        internal async Task<(FastWindow, FastWindowViewModel, FastWindowViewModelDependencies)> CreateFastWindowAsync()
        {
            FastWindow window = await _dispatcher.InvokeAsync(() => new FastWindow());
            await _dispatcher.InvokeAsync(window.Show);
            FastWindowViewModelDependencies fastWindowViewModelDependencies = _getFastWindowViewModelDependencies.Invoke();

            fastWindowViewModelDependencies.DpiCorrector = new WPFDpiCorrector(window, _dispatcher);
            fastWindowViewModelDependencies.SetImage = new WPFDropImageFile(window);
            fastWindowViewModelDependencies.IWindowPositionUpdater = new Win32WPFWindowPositionUpdater(window);

            FastWindowViewModel viewModel = await _dispatcher.InvokeAsync(() => CreateViewModelFastWindow(fastWindowViewModelDependencies));
            await _dispatcher.InvokeAsync(() =>
            {
                window.DataContext = viewModel;
                ((IViewFor)window).ViewModel = viewModel; // не забывать приводить к интерфейсу для активации привязок
            });
            return (window, viewModel, fastWindowViewModelDependencies);
        }
        private FastWindowViewModel CreateViewModelFastWindow(FastWindowViewModelDependencies fastWindowViewModelDependencies) =>
        _dispatcher.Invoke
        (() =>
         new FastWindowViewModel
         (
          fastWindowViewModelDependencies.IGetImage,
          fastWindowViewModelDependencies.IWindowPositionUpdater!,
          fastWindowViewModelDependencies.DpiCorrector!,
          fastWindowViewModelDependencies.SetImage!
         )
        );
    }
    internal class FastWindowViewModelDependencies : IDisposable
    {
        internal bool IsDisposed = false;
        private IGetImage _iGetImage;
        private IWindowPositionUpdater? _iWindowPositionUpdater;
        private WPFDpiCorrector? _dpiCorrector;
        private WPFDropImageFile? _setImage;

        internal FastWindowViewModelDependencies(IGetImage imageProvider)
        {
            _iGetImage = imageProvider;
        }
        internal IGetImage IGetImage { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _iGetImage; } init => _iGetImage = value; }
        internal IWindowPositionUpdater? IWindowPositionUpdater { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _iWindowPositionUpdater; } set => _iWindowPositionUpdater = value; }
        internal WPFDpiCorrector? DpiCorrector { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _dpiCorrector; } set => _dpiCorrector = value; }
        internal WPFDropImageFile? SetImage { get { ObjectDisposedException.ThrowIf(IsDisposed, this); return _setImage; } set => _setImage = value; }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            SetImage?.Dispose();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

