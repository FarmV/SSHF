using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using R3;
using System.Windows.Threading;


namespace FVH.SSHF.FastWindowArea
{
    public partial class FastWindowViewModel  
    {
        private readonly IGetImage _imageProvider;
        private readonly IWindowPositionUpdater _windowPositionUpdater;
        private readonly WPFDpiCorrector _dpiCorrector;
        private readonly WPFDropImageFile _setImage;
        private readonly BindableReactiveProperty<ImageSource?> _imageBackground = new BindableReactiveProperty<ImageSource?>();
        private CancellationTokenSource _updateWindowCancellationToken = new CancellationTokenSource();
        private readonly BindableReactiveProperty<bool> _blockRefresh = new BindableReactiveProperty<bool>();
        private bool _isCancellingUpdate = false;
        private readonly BindableReactiveProperty<bool> _dropCondition = new BindableReactiveProperty<bool>(false);
        private readonly BindableReactiveProperty<bool> _dragMoveCondition = new BindableReactiveProperty<bool>(true);
        private readonly BindableReactiveProperty<double> _width = new BindableReactiveProperty<double>(0);
        private readonly BindableReactiveProperty<double> _height = new BindableReactiveProperty<double>(0);
        private readonly BindableReactiveProperty<Visibility> _visibleCondition = new BindableReactiveProperty<Visibility>(Visibility.Hidden);

#pragma warning disable CS8618 // Empty class constructor for designer only
        public FastWindowViewModel()
#pragma warning restore CS8618
        {
            if(App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");
        }
        public FastWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, WPFDpiCorrector dpiCorrector, WPFDropImageFile setImage)
        {           
            _imageProvider = imageProvider;
            _windowPositionUpdater = windowPositionUpdater;
            _dpiCorrector = dpiCorrector;
            _setImage = setImage;

            RefreshWindowInvoke = new R3.ReactiveCommand(executeAsync: async (_, _) => 
            {
                if(BlockRefresh.CurrentValue is true) return;
                await WindowUpdate();
            }, AwaitOperation.Drop);
            StopWindowUpdater = new R3.ReactiveCommand(executeAsync: async (_, _) => await StopUpdateWindow(), AwaitOperation.Drop);
            SetNewImage = new R3.ReactiveCommand(executeAsync: async (_, _) => await SetNewBackgroundImage(), AwaitOperation.Drop);
            SwitchBlockRefreshWindow = new ReactiveCommand((_) => SwitchBlockRefresh());
            HideWindow = new R3.ReactiveCommand(executeAsync: async (_,_) => await Hide().ConfigureAwait(false), AwaitOperation.Drop);
            ShowWindow = new R3.ReactiveCommand((_) => Show());
            DragMoveWindow = new R3.ReactiveCommand(executeAsync: async (_, _) =>
            {
                if(DragMoveCondition.CurrentValue is false) return;
                await DragMove().ConfigureAwait(false);
            }, AwaitOperation.Drop);
            DropImage = new ReactiveCommand<object>((object data) =>
            {
                if(DropCondition.Value is false) return;
                DropWindowImage(data);   
            });                      
            MsScreenClipInvoke = new ReactiveCommand(executeAsync: async (_, _) => await InvokeMsScreenClip(), AwaitOperation.Drop);

            _ = dpiCorrector.ChangeDpiCurrentWindow.Subscribe((DpiScale dpiScale) => SetNewImageAndWindowSizeDPI(ref dpiScale));
        }
        public R3.ReactiveCommand RefreshWindowInvoke { get; private init; }
        public R3.ReactiveCommand StopWindowUpdater { get; private init; }
        public R3.ReactiveCommand SetNewImage { get; private init; }
        public R3.ReactiveCommand SwitchBlockRefreshWindow { get; private init; }
        public R3.ReactiveCommand HideWindow { get; private init; }
        public R3.ReactiveCommand ShowWindow { get; private init; }
        public R3.ReactiveCommand DragMoveWindow { get; private init; }
        public R3.ReactiveCommand<object> DropImage { get; private init; }
        public R3.ReactiveCommand MsScreenClipInvoke { get; private init; }
        public BindableReactiveProperty<bool> DropCondition => _dropCondition;
        public void SetDropCondition(bool newCondition)
        {
            if(_dropCondition.CurrentValue == newCondition) return;
            if(Application.Current.Dispatcher.CheckAccess() is true) _dropCondition.Value = newCondition; 
            else _ = Application.Current.Dispatcher.Invoke(() => _dropCondition.Value = newCondition);
        }
        public IWindowPositionUpdater WindowPositionUpdater => _windowPositionUpdater;       
        public BindableReactiveProperty<bool> BlockRefresh => _blockRefresh;
        public BindableReactiveProperty<ImageSource?> BackgroundImage => _imageBackground;     
        public BindableReactiveProperty<double> Height => _height;
        public BindableReactiveProperty<double> Width => _width;
        public BindableReactiveProperty<bool> DragMoveCondition => _dragMoveCondition;
        public void SetDragMoveCondition(bool newCondition)
        {
            if(_dragMoveCondition.CurrentValue == newCondition) return;
            if(Application.Current.Dispatcher.CheckAccess() is true) _dragMoveCondition.Value = newCondition;
            else _ = Application.Current.Dispatcher.Invoke(() => _dragMoveCondition.Value = newCondition);
        }
        public BindableReactiveProperty<Visibility> VisibleCondition => _visibleCondition;       
        private async Task WindowUpdate()
        {
            if(_windowPositionUpdater.IsUpdateWindow is true) return;
            if(_isCancellingUpdate is true) return;
            else
            {
                if(_updateWindowCancellationToken.IsCancellationRequested is true) throw new InvalidOperationException();
                if(Application.Current.Dispatcher.CheckAccess() is true) await Task.Run(async () => await _windowPositionUpdater.UpdateWindowPos(_updateWindowCancellationToken.Token)).ConfigureAwait(false);
                else await _windowPositionUpdater.UpdateWindowPos(_updateWindowCancellationToken.Token);
            }           
        }
        public bool CanExecuteStopRefreshWindow() 
        {
            if(_windowPositionUpdater.IsUpdateWindow is false || _isCancellingUpdate is true) return false;
            else { return true; }
        }
        private async Task StopUpdateWindow()
        {
            if(_windowPositionUpdater.IsUpdateWindow is false || _isCancellingUpdate is true) return;
            _isCancellingUpdate = true;
            _updateWindowCancellationToken.Cancel();
            await Task.Run(() => 
            {
                TimeSpan timeout = TimeSpan.FromSeconds(10);
                if(System.Threading.SpinWait.SpinUntil(() => _windowPositionUpdater.IsUpdateWindow is false, timeout) is not true) 
                {
                    string msEx = $"Safety timeout {nameof(StopUpdateWindow)}";
                    AppHelper.DebugExceptionFormat(ref msEx, new System.Diagnostics.StackTrace());
                    throw new TimeoutException(msEx);
                }; 
            });
            _updateWindowCancellationToken = new CancellationTokenSource();
            _isCancellingUpdate = false;          
        }
        private void SetNewImageAndWindowSizeDPI(ref readonly DpiScale dpiScale) //todo обдумать нужно ли и как трансформировать
        {
            if(_imageBackground.CurrentValue == default) return;
            double height = _imageBackground.Value!.Height / dpiScale.DpiScaleY;
            double width = _imageBackground.Value!.Width / dpiScale.DpiScaleX;
            _imageBackground.ForceNotify();
            _height.Value = height;
            _width.Value = width;
        }
        private async Task SetNewBackgroundImage()
        {
            if(await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            DpiScale dpi = _dpiCorrector.GetCurrentDPI();

            double height = image.Height / dpi.DpiScaleY;
            double width = image.Width / dpi.DpiScaleX;
            ImageSource currentImage = image;

            if(_height.Value != height) _height.Value = height;
            if(_width.Value != width) _width.Value = width;
            if(_imageBackground.Value != currentImage) _imageBackground.Value = currentImage;
        }
        private void SwitchBlockRefresh() => _blockRefresh.Value = !BlockRefresh.Value;
        private async Task Hide()
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;
            if(dispatcher.CheckAccess() is true) _visibleCondition.Value = Visibility.Hidden;
            else _ = await dispatcher.InvokeAsync(() => _visibleCondition.Value = Visibility.Hidden, DispatcherPriority.Render);
        }
        private void Show()
        {
            if(MsScreenClip.IsEnableProcessHost() is true) return;
            if(Application.Current.Dispatcher.CheckAccess() is true) VisibleCondition.Value = Visibility.Visible;
            else _ = Application.Current.Dispatcher.Invoke(() => VisibleCondition.Value = Visibility.Visible);            
        }
        private Task DragMove() => _windowPositionUpdater.DragMove();       
        private void DropWindowImage(object ev)
        {
            if(_imageBackground.Value is not ImageSource img) return;
            if(_windowPositionUpdater.IsUpdateWindow is true) return;      
            if(Mouse.LeftButton is not MouseButtonState.Pressed) return;
            _setImage.SaveImageFromDrop(ev, img);
        }
        private async Task InvokeMsScreenClip() 
        {
           if(WindowPositionUpdater.IsUpdateWindow is true) await StopUpdateWindow();
           MsScreenClip.Invoke();
           HideWindow.Execute(Unit.Default);
        }      
    }
}