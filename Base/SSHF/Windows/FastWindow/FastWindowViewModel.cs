using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;

using R3;


namespace FVH.SSHF.FastWindowArea
{
    public partial class FastWindowViewModel
    {
        private readonly ImageProvider     _imageProvider;
        private readonly WPFDpiCorrector   _dpiCorrector;
        private readonly WPFDropImageFile  _setImage;
        private readonly MsScreenClip  _msScreenClip;

#pragma warning disable CS8618 // Empty class constructor for designer only
        public FastWindowViewModel()
#pragma warning restore CS8618
        {
            if(App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");
        }
        public FastWindowViewModel(ImageProvider imageProvider, Win32WPFWindowPositionManager positionManager, WPFDpiCorrector dpiCorrector, WPFDropImageFile setImage, MsScreenClip msScreenClip)
        {
            _imageProvider   = imageProvider;
            _positionManager = positionManager;
            _dpiCorrector    = dpiCorrector;
            _setImage        = setImage;
            _msScreenClip    = msScreenClip;

            SwitchBlockRefreshCommand = new R3.ReactiveCommand((_) => SwitchBlockRefresh());

            ShowWindowCommand = new R3.ReactiveCommand((_) => ShowWindow());
            HideWindowCommand = new R3.ReactiveCommand((_) => HideWindow());

            WindowUpdaterCommand = new R3.ReactiveCommand(async (_, _) => await WindowUpdater(), AwaitOperation.Drop);
            StopWindowUpdaterCommand = new R3.ReactiveCommand(async (_, _) => await StopUpdateWindow(), AwaitOperation.Drop);

            InvokeMsScreenClipCommand = new R3.ReactiveCommand((_) => InvokeMsScreenClip());

            SetNewImageCommand = new R3.ReactiveCommand(async (_) => await SetNewImage());

            DragMoveWindowCommand = new R3.ReactiveCommand(async (_, _) => await DragMoveWindow(), AwaitOperation.Drop);

            DropImageCommand = new R3.ReactiveCommand<object>(DropImage);

            _ = dpiCorrector.ChangeDpiCurrentWindow.Subscribe((DpiScale dpiScale) => SetNewImageAndWindowSizeDPI(in dpiScale));
        }

        private readonly BindableReactiveProperty<bool> _blockRefresh = new BindableReactiveProperty<bool>();
        public BindableReactiveProperty<bool> BlockRefresh => _blockRefresh;
        public void SwitchBlockRefresh() => _blockRefresh.Value = !BlockRefresh.Value;
        public R3.ReactiveCommand SwitchBlockRefreshCommand { get; private init; }


        private readonly BindableReactiveProperty<Visibility>  _visibleCondition = new BindableReactiveProperty<Visibility>(Visibility.Hidden);
        public BindableReactiveProperty<Visibility> VisibleCondition => _visibleCondition;
        public void ShowWindow() => Application.Current.Dispatcher.Invoke(new Action(() => VisibleCondition.Value = Visibility.Visible));
        public void HideWindow() 
        {
            Application.Current.Dispatcher.Invoke(new Action(() => VisibleCondition.Value = Visibility.Hidden));
        }
        public R3.ReactiveCommand ShowWindowCommand { get; private init; }
        public R3.ReactiveCommand HideWindowCommand { get; private init; }


        private readonly Win32WPFWindowPositionManager   _positionManager;
        private          CancellationTokenSource  _updateWindowCancellationToken = new CancellationTokenSource();
        public           Win32WPFWindowPositionManager PositionManager => _positionManager;
        private          bool                     _isCancellingUpdate            = false;
        public bool CanExecuteStopRefreshWindow()
        {
            if(_positionManager.IsUpdateWindow is true && _isCancellingUpdate is false) return true;
            else { return false; }
        }
        public async Task WindowUpdater()
        {
            if(BlockRefresh.CurrentValue is true) return;
            if(_positionManager.IsUpdateWindow is true) return;
            if(_isCancellingUpdate is true) return;
            else
            {
                if(_updateWindowCancellationToken.IsCancellationRequested is true) Throw(); [DoesNotReturn] static void Throw() => throw new InvalidOperationException();
                await _positionManager.UpdateWindowPositionRelativeToCursor(_updateWindowCancellationToken.Token);
            }
        }
        public R3.ReactiveCommand WindowUpdaterCommand { get; private init; }
        public async Task StopUpdateWindow()
        {
            if(_positionManager.IsUpdateWindow is false ||  _isCancellingUpdate is true) return;
            await Task.Run(() =>
            {
                Volatile.Write(ref _isCancellingUpdate, true);
                _updateWindowCancellationToken.Cancel();

                TimeSpan timeout = TimeSpan.FromSeconds(10);
                if(System.Threading.SpinWait.SpinUntil(() => _positionManager.IsUpdateWindow is false, timeout) is not true)
                {
                    string msEx = $"Safety timeout {nameof(StopUpdateWindow)}";
#if DEBUG
                    AppHelper.DebugExceptionFormat(ref msEx, new System.Diagnostics.StackTrace(true));
#endif
                    Throw(msEx); [DoesNotReturn] static void Throw(string msEx) => throw new TimeoutException(msEx);
                }

                _updateWindowCancellationToken = new CancellationTokenSource();
                Volatile.Write(ref _isCancellingUpdate, false);
            }).ConfigureAwait(false);
        }
        public R3.ReactiveCommand StopWindowUpdaterCommand { get; private init; }

        public void InvokeMsScreenClip() => _msScreenClip.Invoke();       
        public R3.ReactiveCommand InvokeMsScreenClipCommand { get; private init; }

        private readonly BindableReactiveProperty<double>                _height = new BindableReactiveProperty<double>(0);
        public BindableReactiveProperty<double> Height => _height;
        private readonly BindableReactiveProperty<double>                _width  = new BindableReactiveProperty<double>(0);
        public BindableReactiveProperty<double> Width => _width;
        private readonly BindableReactiveProperty<ImageSource?>          _imageBackground = new BindableReactiveProperty<ImageSource?>();
        public BindableReactiveProperty<ImageSource?> BackgroundImage => _imageBackground;
        public async Task<bool> SetNewImage()
        {
            if(await _imageProvider.GetImageFromClipboard() is not BitmapSource image) return false;
            DpiScale dpi = _dpiCorrector.GetCurrentDPI();

            double height = image.PixelHeight / dpi.DpiScaleY;
            double width  = image.PixelWidth  / dpi.DpiScaleX;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _height.Value = height;
                _width.Value = width;
                _imageBackground.Value = image;
            });
            return true;
        }
        public R3.ReactiveCommand SetNewImageCommand { get; private init; }
        private void SetNewImageAndWindowSizeDPI(in DpiScale dpiScale) //todo обдумать нужно ли и как трансформировать
        {
            if(_imageBackground.CurrentValue == default) return;
            double height = _imageBackground.Value!.Height / dpiScale.DpiScaleY;
            double width = _imageBackground.Value!.Width / dpiScale.DpiScaleX;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _imageBackground.ForceNotify();
                _height.Value = height;
                _width.Value = width;
            });
        }


        private readonly BindableReactiveProperty<bool>  _dragMoveCondition = new BindableReactiveProperty<bool>(true);
        public BindableReactiveProperty<bool> DragMoveCondition => _dragMoveCondition;
        public void SetDragMoveCondition(bool newCondition) => _dragMoveCondition.Value = newCondition;
        public async ValueTask DragMoveWindow()
        {
            if(DragMoveCondition.CurrentValue is false) return;
            await _positionManager.DragMove();
        }
        public R3.ReactiveCommand DragMoveWindowCommand { get; private init; }


        private readonly BindableReactiveProperty<bool>  _dropCondition = new BindableReactiveProperty<bool>(false);
        public BindableReactiveProperty<bool> DropCondition => _dropCondition;
        public void SetDropCondition(bool newCondition)
        {
            if(_dropCondition.CurrentValue == newCondition) return;
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => _dropCondition.Value = newCondition));        
        }


        private void DropImage(object ev)
        {
            if(DropCondition.Value is false) return;
            if(_imageBackground.Value is not BitmapSource bitmapSource) return;
            if(_positionManager.IsUpdateWindow is true) return;
            if(Mouse.LeftButton is not MouseButtonState.Pressed) return;
            _setImage.SaveImageFromDrop(ev, bitmapSource);
        }
        public R3.ReactiveCommand<object> DropImageCommand { get; private init; }
    }
}