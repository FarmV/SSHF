using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Threading;
using System.Windows.Media;
using System.Reflection;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Windows.Threading;
using SSHF.Infrastructure;
using SSHF.Infrastructure.Interfaces;

namespace SSHF.ViewModels.MainWindowViewModel
{
    public partial class MainWindowViewModel : ReactiveObject
    {
        private readonly IGetImage _imageProvider;
        private readonly IWindowPositionUpdater _windowPositionUpdater;
        private readonly DpiCorrector _dpiCorrector;
        private readonly WPFDropImageFile _setImage;
        private Visibility _visibleCondition = Visibility.Hidden;
        private ImageSource? _imageBackground;
        private CancellationTokenSource _updateWindowCancellationToken = new CancellationTokenSource();
        private bool _blockRefresh = false;
        private bool _ICancellingUpdate = false;
        private bool _dropCondition = false;
        private bool _dragMoveCondition = false;
        private double _width = 0;
        private double _height = 0;

#pragma warning disable CS8618 // Empty class constructor for designer only
        public MainWindowViewModel()
#pragma warning restore CS8618
        {
            if (App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");
        }
        public MainWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector dpiCorrector, WPFDropImageFile setImage)
        {
            _imageProvider = imageProvider;
            _windowPositionUpdater = windowPositionUpdater;
            _dpiCorrector = dpiCorrector;
            _setImage = setImage;
            RefreshWindowInvoke = ReactiveCommand.Create(WindowUpdate, this.WhenAnyValue(x => x.BlockRefresh, (bool blockRefresh) => blockRefresh is false));
            StopWindowUpdater = ReactiveCommand.Create(StopUpdateWindow);
            SetNewImage = ReactiveCommand.CreateFromTask(SetNewBackgoundImage);
            SwithBlockRefreshWindow = ReactiveCommand.Create(SwitchBlockRefresh);
            HideWindow = ReactiveCommand.Create(Hide);
            ShowWindow = ReactiveCommand.Create(Show);
            DragMoveWindow = ReactiveCommand.Create(DragMove, this.WhenAnyValue(x => x.DragMoveCondition));
            DropImage = ReactiveCommand.Create<object>(DropWindowImage, this.WhenAnyValue(x => x.DropCondition));
        }
        public ReactiveCommand<Unit, Unit> RefreshWindowInvoke { get; private set; }
        public ReactiveCommand<Unit, Unit> StopWindowUpdater { get; private set; }
        public ReactiveCommand<Unit, Unit> SetNewImage { get; private set; }
        public ReactiveCommand<Unit, Unit> SwithBlockRefreshWindow { get; private set; }
        public ReactiveCommand<Unit, Unit> HideWindow { get; private set; }
        public ReactiveCommand<Unit, Unit> ShowWindow { get; private set; }
        public ReactiveCommand<Unit, Unit> DragMoveWindow { get; private set; }
        public ReactiveCommand<object, Unit> DropImage { get; private set; }
        private void WindowUpdate()
        {
            if (_windowPositionUpdater.IsUpdateWindow is true) return;
            if (_ICancellingUpdate is true) return;
            else
            {
                if (_updateWindowCancellationToken.IsCancellationRequested is true) throw new InvalidOperationException();
                _windowPositionUpdater.UpdateWindowPos(_updateWindowCancellationToken.Token);
            }
        }
        private void DropWindowImage(object ev)
        {
            if (BackgroundImage is not ImageSource img) return;
            if (_windowPositionUpdater.IsUpdateWindow is true) return;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) is not true) return; //todo обдумать
            _setImage.SaveImageFromDrop(ev, img);
        }
        private void DragMove() => _windowPositionUpdater.DargMove();
        private void Hide() => VisibleCondition = Visibility.Hidden;
        private void Show() => VisibleCondition = Visibility.Visible;
        private void StopUpdateWindow()
        {
            if (_windowPositionUpdater.IsUpdateWindow is false) return;
            _ICancellingUpdate = true;
            _updateWindowCancellationToken.Cancel();
            while (System.Threading.SpinWait.SpinUntil(() => _windowPositionUpdater.IsUpdateWindow is false, TimeSpan.FromMilliseconds(300))) ;
            if (_windowPositionUpdater.IsUpdateWindow is false) throw new InvalidOperationException();
            _updateWindowCancellationToken = new CancellationTokenSource();
            _ICancellingUpdate = false;
        }
        private async Task SetNewBackgoundImage()
        {
            if (await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            DpiSacaleMonitor dpi = _dpiCorrector.GetCurretDPI();
            Height = image.Height / dpi.DpiScaleY;
            Width = image.Width / dpi.DpiScaleX;
            BackgroundImage = image;
        }
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
        private void SwitchBlockRefresh()
        {
            BlockRefresh = !BlockRefresh;
        }
        public ImageSource? BackgroundImage
        {
            get => _imageBackground;
            private set
            {
                this.RaiseAndSetIfChanged(ref _imageBackground, value);
            }
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
        private static Uri GetUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
        private async Task GetImageImageFromClipboard()
        {
            if (await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            BackgroundImage = image;
        }
        private async Task StartRefreshWindow(CancellationToken token)
        {
            if (WindowPositionUpdater.IsUpdateWindow is true) return;
            if (BlockRefresh is true) return;
            await WindowPositionUpdater.UpdateWindowPos(token);
        }
        private async Task RefreshWindow()
        {

            // if (_updatePositionWindow.IsUpdateWindow is true) return;

            //await _thisWindow.Dispatcher.InvokeAsync(async () =>
            //{
            //    if (IsRefreshWindow is true) return;
            //    if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL)) return;

            //    try
            //    {
            //        if (System.Windows.Clipboard.ContainsImage() is true)
            //        {
            //            BitmapSource bitSor = System.Windows.Clipboard.GetImage();
            //            RenderOptions.SetBitmapScalingMode(bitSor, BitmapScalingMode.NearestNeighbor);

            //            using MemoryStream createFileFromImageBuffer = new MemoryStream();      
            //            BitmapEncoder encoder = new PngBitmapEncoder();
            //            BitmapFrame ccc = BitmapFrame.Create(bitSor);
            //            encoder.Frames.Add(BitmapFrame.Create(bitSor));
            //            encoder.Save(createFileFromImageBuffer);
            //            BitmapImage image = new BitmapImage();
            //            image.BeginInit();
            //            image.StreamSource = createFileFromImageBuffer;
            //            image.EndInit();

            //            var curPos = SSHF.Infrastructure.SharedFunctions.CursorFunctions.GetCursorPosition();

            //            BackgroundImage = image;
            //            _thisWindow.Height = image.Height;
            //            _thisWindow.Width = image.Width;
            //            if (BlockRefresh is not true)
            //            {
            //                _thisWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            //                _thisWindow.Top = curPos.Y;
            //                _thisWindow.Left = curPos.X;
            //            }
            //            _thisWindow.Show();
            //            _thisWindow.Focus();
            //        }
            //        else
            //        {
            //            _thisWindow.Show();
            //            _thisWindow.Focus();
            //        }

            //        CancellationTokenSource tokenSource = new CancellationTokenSource();

            //        void MouseInput(object? sender, Infrastructure.Algorithms.Input.RawInputEvent e)
            //        {
            //            if (e.Data is RawInputKeyboardData)
            //            {
            //                if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL) is true)
            //                {
            //                    tokenSource.Cancel();
            //                    App.Input -= MouseInput;
            //                    return;
            //                }
            //            }
            //            if (e.Data is not RawInputMouseData data || data.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
            //            else
            //            {
            //                tokenSource.Cancel();
            //                App.Input -= MouseInput;
            //            }

            //        }
            //        _ = Task.Run(() =>
            //        {
            //            App.Input += MouseInput;
            //        }).ConfigureAwait(false);

            //        if (BlockRefresh is true) return;
            //        IsRefreshWindow = true;
            //        await _thisWindow.Dispatcher.InvokeAsync(new Action(async () => { await Win32ApiWindow.RefreshWindowPositin.RefreshWindowPosCursor(App.WindowsIsOpen[App.GetMyMainWindow].Key, tokenSource.Token); }),
            //            System.Windows.Threading.DispatcherPriority.Render);

            //        IsRefreshWindow = false;
            //    }
        }



        //public RelayCommand GetThisWindow => _getThisWindow = _getThisWindow is not null ? _getThisWindow :
        //                     new RelayCommand(obj => _thisWindow = obj is not RoutedEventArgs ev ?
        //                      throw new ArgumentException("Неверный входной параметр", typeof(RoutedEventArgs).ToString()) :
        //                       ev.Source is not Window win ? throw new InvalidOperationException() : win);


        //public RelayCommand NotClosingCommand => _closingCommand = _closingCommand is not null ? _closingCommand : new RelayCommand(obj =>
        //{
        //    _ = obj is not CancelEventArgs eventArgs ? throw new ArgumentException() : eventArgs.Cancel = true;
        //    _thisWindow?.Hide();
        //});


        //public RelayCommand DoubleClickHideWindowCommand => _doubleClickHideWindowCommand = _doubleClickHideWindowCommand is not null ?
        //                     _doubleClickHideWindowCommand : new RelayCommand(obj => _thisWindow?.Hide());




        //public RelayCommand MouseWheel => _mouseWheel = _mouseWheel is not null ? _mouseWheel : new RelayCommand(obj =>
        //{
        //    int res = obj is not MouseWheelEventArgs eventArgs ? throw new ArgumentException() : eventArgs.Delta;

        //    if (res > 0)
        //    {
        //        if (_thisWindow is null) throw new NullReferenceException(nameof(_thisWindow));

        //        _thisWindow.Width += 20;
        //        _thisWindow.Height += 20;

        //        eventArgs.Handled = true;
        //        return;
        //    }
        //    if (res < 0)
        //    {
        //        if (_thisWindow is null) throw new NullReferenceException(nameof(_thisWindow));
        //        if (_thisWindow.Width - 20 < 0 || _thisWindow.Height - 20 < 0) return;
        //        _thisWindow.Width -= 20;
        //        _thisWindow.Height -= 20;
        //        eventArgs.Handled = true;
        //        return;
        //    }

        //});




        //private void ClearTMP() => Array.ForEach(deleteTMP.ToArray(), (x) => { if (File.Exists(x) is true) File.Delete(x); else deleteTMP.Remove(x); });
        //private static readonly List<string> deleteTMP = new List<string>();
        //private RelayCommand? _dropImage;
        //public RelayCommand DropImage => _dropImage = _dropImage is not null ? _dropImage : new RelayCommand(obj =>
        //{
        //    if (IsRefreshWindow is true) return;
        //    if (obj is not MouseEventArgs Event) throw new InvalidOperationException();

        //    if (( Event.MouseDevice.LeftButton == MouseButtonState.Pressed ) & KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL) is true)
        //    {
        //        string temp = Path.GetTempFileName();
        //        FileInfo fileInfo = new FileInfo(temp)
        //        {
        //            Attributes = FileAttributes.Temporary
        //        };
        //        temp = Path.ChangeExtension(Path.GetTempFileName(), "png");
        //        using (FileStream createFile = new FileStream(temp, FileMode.OpenOrCreate))
        //        {
        //            BitmapEncoder encoder = new PngBitmapEncoder();
        //            encoder.Frames.Add(BitmapFrame.Create(_ImageBackground));
        //            encoder.Save(createFile);

        //        }

        //        string[] arrayDrops = new string[] { temp };
        //        DataObject dataObject = new DataObject(DataFormats.FileDrop, arrayDrops);
        //        dataObject.SetData(DataFormats.StringFormat, dataObject);

        //        if (IsRefreshWindow is true) return;
        //        DragDrop.DoDragDrop((System.Windows.Controls.Border)Event.Source, dataObject, DragDropEffects.Copy);

        //        ClearTMP();
        //        deleteTMP.Add(temp);
        //    }

        //});







        //private RelayCommand? _refreshWindow;


        //public RelayCommand InvoceRefreshWindow => _refreshWindow = _refreshWindow is not null ? _refreshWindow : new RelayCommand(async obj =>
        //{
        //    if (_thisWindow is null) throw new NullReferenceException();
        //    await _thisWindow.Dispatcher.InvokeAsync(async () =>
        //    {
        //        if (IsRefreshWindow is true) return;
        //        if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL)) return;

        //        try
        //        {
        //            if (System.Windows.Clipboard.ContainsImage() is true)
        //            {
        //                BitmapSource bitSor = System.Windows.Clipboard.GetImage();
        //                RenderOptions.SetBitmapScalingMode(bitSor, BitmapScalingMode.NearestNeighbor);

        //                using MemoryStream createFileFromImageBuffer = new MemoryStream();         //todo переехать в интерфейс конвертации изображений
        //                BitmapEncoder encoder = new PngBitmapEncoder();
        //                BitmapFrame ccc = BitmapFrame.Create(bitSor);
        //                encoder.Frames.Add(BitmapFrame.Create(bitSor));
        //                encoder.Save(createFileFromImageBuffer);
        //                BitmapImage image = new BitmapImage();
        //                image.BeginInit();
        //                image.StreamSource = createFileFromImageBuffer;
        //                image.EndInit();

        //                var curPos = SSHF.Infrastructure.SharedFunctions.CursorFunctions.GetCursorPosition();

        //                BackgroundImage = image;
        //                _thisWindow.Height = image.Height;
        //                _thisWindow.Width = image.Width;
        //                if (BlockRefresh is not true)
        //                {
        //                    _thisWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        //                    _thisWindow.Top = curPos.Y;
        //                    _thisWindow.Left = curPos.X;
        //                }
        //                _thisWindow.Show();
        //                _thisWindow.Focus();
        //            } else
        //            {
        //                _thisWindow.Show();
        //                _thisWindow.Focus();
        //            }

        //            CancellationTokenSource tokenSource = new CancellationTokenSource();

        //            void MouseInput(object? sender, Infrastructure.Algorithms.Input.RawInputEvent e)
        //            {
        //                if (e.Data is RawInputKeyboardData)
        //                {
        //                    if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL) is true)
        //                    {
        //                        tokenSource.Cancel();
        //                        App.Input -= MouseInput;
        //                        return;
        //                    }
        //                }
        //                if (e.Data is not RawInputMouseData data || data.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
        //                else
        //                {
        //                    tokenSource.Cancel();
        //                    App.Input -= MouseInput;
        //                }

        //            }
        //            _ = Task.Run(() =>
        //            {
        //                App.Input += MouseInput;
        //            }).ConfigureAwait(false);

        //            if (BlockRefresh is true) return;
        //            IsRefreshWindow = true;
        //            await _thisWindow.Dispatcher.InvokeAsync(new Action(async () => { await Win32ApiWindow.RefreshWindowPositin.RefreshWindowPosCursor(App.WindowsIsOpen[App.GetMyMainWindow].Key, tokenSource.Token); }),
        //                System.Windows.Threading.DispatcherPriority.Render);

        //            IsRefreshWindow = false;

        //        } catch (Exception) { }
        //    }, System.Windows.Threading.DispatcherPriority.Render);



        //});


    }
}
