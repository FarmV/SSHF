using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;




using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using R3;
using System.ComponentModel;
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
        private readonly BindableReactiveProperty<bool> _dropCondition = new BindableReactiveProperty<bool>();
        private readonly BindableReactiveProperty<bool> _dragMoveCondition = new BindableReactiveProperty<bool>();
        private readonly BindableReactiveProperty<double> _width = new BindableReactiveProperty<double>(0);
        private readonly BindableReactiveProperty<double> _height = new BindableReactiveProperty<double>(0);
        private readonly BindableReactiveProperty<Visibility> _visibleCondition = new BindableReactiveProperty<Visibility>(Visibility.Hidden);

        // private readonly BindableReactiveProperty<Visibility> _visibleCondition = new BindableReactiveProperty<Visibility>(Visibility.Hidden);
        //public BindableReactiveProperty<ImageSource?> BackgroundImage { get; } = new BindableReactiveProperty<ImageSource?>();

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
                if(BlockRefresh.Value is true) return;
                await WindowUpdate();
            }, AwaitOperation.Drop);
            StopWindowUpdater = new R3.ReactiveCommand(executeAsync: async (_, _) => await StopUpdateWindow(), AwaitOperation.Drop);
            SetNewImage = new R3.ReactiveCommand(executeAsync: async (_, _) => await SetNewBackgroundImage(), AwaitOperation.Drop);
            SwitchBlockRefreshWindow = new ReactiveCommand((_) => SwitchBlockRefresh());
            HideWindow = new R3.ReactiveCommand(executeAsync: async (_,_) => await Hide(), AwaitOperation.Drop);
            ShowWindow = new R3.ReactiveCommand((_) => Show());
            DragMoveWindow = new R3.ReactiveCommand(executeAsync: async (_, _) =>
            {
                if(DragMoveCondition.Value is false) return;
                await DragMove();
            }, AwaitOperation.Drop);
            DropImage = new ReactiveCommand<object, R3.Unit>((data) =>
            {
                if(DropCondition.Value is false) return R3.Unit.Default;
                DropWindowImage(data);
                return R3.Unit.Default;
            });                      
            MsScreenClipInvoke = new ReactiveCommand(executeAsync: async (_, _) => await InvokeMsScreenClip(), AwaitOperation.Drop);            
        }

        BindableReactiveProperty<ImageSource?>  _rp3 = new BindableReactiveProperty<ImageSource?>();
        BindableReactiveProperty<ImageSource?> _rp4 => _rp3;

        public R3.ReactiveCommand RefreshWindowInvoke { get; private init; }
        public R3.ReactiveCommand StopWindowUpdater { get; private init; }
        public R3.ReactiveCommand SetNewImage { get; private init; }
        public R3.ReactiveCommand SwitchBlockRefreshWindow { get; private init; }
        public R3.ReactiveCommand HideWindow { get; private init; }
        public R3.ReactiveCommand ShowWindow { get; private init; }
        public R3.ReactiveCommand DragMoveWindow { get; private init; }
        public R3.ReactiveCommand<object, R3.Unit> DropImage { get; private init; }
        public R3.ReactiveCommand MsScreenClipInvoke { get; private init; }

        public BindableReactiveProperty<bool> DropCondition => _dropCondition;
        public void SetDropCondition(bool newCondition)
        {
            if(_dropCondition.Value == newCondition) return;
            _dropCondition.Value = newCondition;
        }
        //public bool DropCondition
        //{
        //    get => _dropCondition;
        //    set => this.RaiseAndSetIfChanged(ref _dropCondition, value);
        //}
        public IWindowPositionUpdater WindowPositionUpdater => _windowPositionUpdater;
        
        public BindableReactiveProperty<bool> BlockRefresh => _blockRefresh;
        //public bool BlockRefresh
        //{
        //    get => _blockRefresh;
        //    private set => this.RaiseAndSetIfChanged(ref _blockRefresh, value);
        //}

        public BindableReactiveProperty<ImageSource?> BackgroundImage => _imageBackground;

        //public ImageSource? BackgroundImage
        //{
        //    get => _imageBackground;
        //    private set => this.RaiseAndSetIfChanged(ref _imageBackground, value);
        //}       
        public BindableReactiveProperty<double> Height => _height;
        //public double Height
        //{
        //    get => _height;
        //    set => this.RaiseAndSetIfChanged(ref _height, value);
        //}
        public BindableReactiveProperty<double> Width => _width;
        //public double Width
        //{
        //    get => _width;
        //    private set => this.RaiseAndSetIfChanged(ref _width, value);
        //}
        public BindableReactiveProperty<bool> DragMoveCondition => _dragMoveCondition;
        public void SetDragMoveCondition(bool newCondition)
        {
            if(_dragMoveCondition.Value == newCondition) return;
            _dragMoveCondition.Value = newCondition;
        }
        //public bool DragMoveCondition
        //{
        //    get => _dragMoveCondition;
        //    set => this.RaiseAndSetIfChanged(ref _dragMoveCondition, value);
        //}
        public BindableReactiveProperty<Visibility> VisibleCondition => _visibleCondition;
        //public Visibility VisibleCondition
        //{
        //    get => _visibleCondition;
        //    set => this.RaiseAndSetIfChanged(ref _visibleCondition, value);
        //}
        private Task WindowUpdate() =>       
        Task.Run(async () =>
        {
           if(_windowPositionUpdater.IsUpdateWindow is true) return;
           if(_isCancellingUpdate is true) return;
           else
           {
               if(_updateWindowCancellationToken.IsCancellationRequested is true) throw new InvalidOperationException();
               await _windowPositionUpdater.UpdateWindowPos(_updateWindowCancellationToken.Token);
           }
        });       
        private async Task StopUpdateWindow()
        {
            if(_windowPositionUpdater.IsUpdateWindow is false || _isCancellingUpdate is true) return;
            _isCancellingUpdate = true;
            _updateWindowCancellationToken.Cancel();
            await Task.Run(() => 
            {
                if(System.Threading.SpinWait.SpinUntil(() => _windowPositionUpdater.IsUpdateWindow is false, TimeSpan.FromSeconds(1.4D)) is not true) 
                {
                    //var test = new TimeoutException();
                    //test.HelpLink =
                    throw new TimeoutException($"{nameof(StopUpdateWindow)}");
                }; 
            });
            _updateWindowCancellationToken = new CancellationTokenSource();
            _isCancellingUpdate = false;
        }
        private async Task SetNewBackgroundImage()
        {
            if(await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            DpiSacaleMonitor dpi = _dpiCorrector.GetCurrentDPI();

            double height = image.Height / dpi.DpiScaleY;
            double width = image.Width / dpi.DpiScaleX;
            ImageSource currentImage = image;

             _height.Value = height;
             _width.Value = width;
            _imageBackground.Value = currentImage;
        }
        private void SwitchBlockRefresh() => _blockRefresh.Value = !BlockRefresh.Value;
        private async Task Hide()
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;
            if(dispatcher.CheckAccess() is true) _visibleCondition.Value = Visibility.Hidden;
            else await dispatcher.InvokeAsync(() => _visibleCondition.Value = Visibility.Hidden, DispatcherPriority.Render);
        }
        private void Show()
        {
            if(MsScreenClip.IsEnableProcessHost() is true) return;
            VisibleCondition.Value = Visibility.Visible;
        }
        private Task DragMove()
        {
            if(_windowPositionUpdater.IsUpdateWindow is true) return Task.CompletedTask;
            _windowPositionUpdater.DragMove().Wait();
            return Task.CompletedTask;
        }
        private void DropWindowImage(object ev)
        {
            if(_imageBackground.Value is not ImageSource img) return;
            if(_windowPositionUpdater.IsUpdateWindow is true) return;
            if(Keyboard.IsKeyDown(Key.LeftCtrl) is not true) return;           
            if(Mouse.LeftButton is not MouseButtonState.Pressed) return;
            _setImage.SaveImageFromDrop(ev, img);
        }
        private async Task InvokeMsScreenClip() 
        {
           if(WindowPositionUpdater.IsUpdateWindow is true) await StopUpdateWindow();
           MsScreenClip.Invoke();
           Thread.Sleep(200); // Чтобы окно оставалось в скриншоте, но убралось и не мешало композиции
           HideWindow.Execute(Unit.Default);
        }      
    }
}
