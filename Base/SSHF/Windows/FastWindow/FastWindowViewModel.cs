using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;
using System.Reactive;

using ReactiveUI;

using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;
using System.Reactive.Linq;

namespace FVH.SSHF.ViewModels.MainWindowViewModel
{
    public partial class FastWindowViewModel : ReactiveObject
    {
        private readonly IGetImage _imageProvider;
        private readonly IWindowPositionUpdater _windowPositionUpdater;
        private readonly DpiCorrector _dpiCorrector;
        private readonly WPFDropImageFile _setImage;
        private Visibility _visibleCondition = Visibility.Hidden;
        private ImageSource? _imageBackground;
        private CancellationTokenSource _updateWindowCancellationToken = new CancellationTokenSource();
        private bool _blockRefresh = false;
        private bool _isCancellingUpdate = false;
        private bool _dropCondition = false;
        private bool _dragMoveCondition = false;
        private double _width = 0;
        private double _height = 0;

#pragma warning disable CS8618 // Empty class constructor for designer only
        public FastWindowViewModel()
#pragma warning restore CS8618
        {
            if(App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");
        }
        public FastWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector dpiCorrector, WPFDropImageFile setImage)
        {
            _imageProvider = imageProvider;
            _windowPositionUpdater = windowPositionUpdater;
            _dpiCorrector = dpiCorrector;
            _setImage = setImage;
            RefreshWindowInvoke = ReactiveCommand.CreateFromTask(WindowUpdate, this.WhenAnyValue(x => x.BlockRefresh, (bool blockRefresh) => blockRefresh is false));
            StopWindowUpdater = ReactiveCommand.CreateFromTask(StopUpdateWindow);
            SetNewImage = ReactiveCommand.CreateFromTask(SetNewBackgroundImage);
            SwitchBlockRefreshWindow = ReactiveCommand.Create(SwitchBlockRefresh);
            HideWindow = ReactiveCommand.Create(Hide);
            ShowWindow = ReactiveCommand.Create(Show);
            DragMoveWindow = ReactiveCommand.CreateFromTask(DragMove, this.WhenAnyValue(x => x.DragMoveCondition));
            DropImage = ReactiveCommand.Create<object>(DropWindowImage, this.WhenAnyValue(x => x.DropCondition));
            MsScreenClipInvoke = ReactiveCommand.Create(InvokeMsScreenClip);
        }
        public ReactiveCommand<Unit, Unit> RefreshWindowInvoke { get; private init; }
        public ReactiveCommand<Unit, Unit> StopWindowUpdater { get; private init; }
        public ReactiveCommand<Unit, Unit> SetNewImage { get; private init; }
        public ReactiveCommand<Unit, Unit> SwitchBlockRefreshWindow { get; private init; }
        public ReactiveCommand<Unit, Unit> HideWindow { get; private init; }
        public ReactiveCommand<Unit, Unit> ShowWindow { get; private init; }
        public ReactiveCommand<Unit, Unit> DragMoveWindow { get; private init; }
        public ReactiveCommand<object, Unit> DropImage { get; private init; }
        public ReactiveCommand<Unit, Unit> MsScreenClipInvoke { get; private init; }
        public bool DropCondition
        {
            get => _dropCondition;
            set => this.RaiseAndSetIfChanged(ref _dropCondition, value);
        }
        public IWindowPositionUpdater WindowPositionUpdater
        {
            get => _windowPositionUpdater;
        }
        public bool BlockRefresh
        {
            get => _blockRefresh;
            private set => this.RaiseAndSetIfChanged(ref _blockRefresh, value);
        }
        public ImageSource? BackgroundImage
        {
            get => _imageBackground;
            private set => this.RaiseAndSetIfChanged(ref _imageBackground, value);
        }
        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }
        public double Width
        {
            get => _width;
            private set => this.RaiseAndSetIfChanged(ref _width, value);
        }
        public bool DragMoveCondition
        {
            get => _dragMoveCondition;
            set => this.RaiseAndSetIfChanged(ref _dragMoveCondition, value);
        }
        public Visibility VisibleCondition
        {
            get => _visibleCondition;
            set => this.RaiseAndSetIfChanged(ref _visibleCondition, value);
        }
        private async Task WindowUpdate()
        {
            await Task.Factory.StartNew(async () =>
            {
                if(_windowPositionUpdater.IsUpdateWindow is true) return;
                if(_isCancellingUpdate is true) return;
                else
                {
                    if(_updateWindowCancellationToken.IsCancellationRequested is true) throw new InvalidOperationException();
                    await _windowPositionUpdater.UpdateWindowPos(_updateWindowCancellationToken.Token);
                }
            });
        }
        private async Task StopUpdateWindow()
        {
            if(_windowPositionUpdater.IsUpdateWindow is false || _isCancellingUpdate is true) return;
            _isCancellingUpdate = true;
            _updateWindowCancellationToken.Cancel();
            await Task.Run(() => { if(System.Threading.SpinWait.SpinUntil(() => _windowPositionUpdater.IsUpdateWindow is false, TimeSpan.FromSeconds(1)) is not true) throw new TimeoutException(); });
            _updateWindowCancellationToken = new CancellationTokenSource();
            _isCancellingUpdate = false;
        }
        private async Task SetNewBackgroundImage()
        {
            if(await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            DpiSacaleMonitor dpi = _dpiCorrector.GetCurrentDPI();
            Height = image.Height / dpi.DpiScaleY;
            Width = image.Width / dpi.DpiScaleX;
            BackgroundImage = image;
        }
        private void SwitchBlockRefresh() => BlockRefresh = !BlockRefresh;
        private void Hide() => VisibleCondition = Visibility.Hidden;
        private void Show()
        {
            if(MsScreenClip.IsEnableProcessHost() is true) return;
            VisibleCondition = Visibility.Visible;
        }
        private async Task DragMove()
        {
            if(_windowPositionUpdater.IsUpdateWindow is true) return;
            await _windowPositionUpdater.DragMove();
        }
        private void DropWindowImage(object ev)
        {
            if(BackgroundImage is not ImageSource img) return;
            if(_windowPositionUpdater.IsUpdateWindow is true) return;
            if(Keyboard.IsKeyDown(Key.LeftCtrl) is not true) return;           
            if(Mouse.LeftButton is not MouseButtonState.Pressed) return;
            _setImage.SaveImageFromDrop(ev, img);
        }
        private void InvokeMsScreenClip() 
        { 
           MsScreenClip.Invoke();
           Thread.Sleep(400); // Чтобы окно оставалось в скриншоте, но убралось и не мешало композиции
           HideWindow.Execute().Wait();
        } 
        // private static Uri GetUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
    }
}
