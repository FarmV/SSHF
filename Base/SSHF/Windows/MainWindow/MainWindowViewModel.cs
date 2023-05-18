using System;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using SSHF.Infrastructure.SharedFunctions;
using System.Threading;
using System.Windows.Media;
using System.Reflection;
using ReactiveUI;
using System.Reactive;
using FVH.Background.Input;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Threading;
using System.Linq;

namespace SSHF.ViewModels.MainWindowViewModel
{
    public partial class MainWindowViewModel : ReactiveObject
    {
        private readonly IGetImage _imageProvider;
        private readonly IWindowPositionUpdater _windowPositionUpdater;
        private ImageSource? _imageBackground;
        private bool _blockRefresh;
        private Visibility _visibleCondition = Visibility.Hidden;
        private double _height;
        private double _width;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private bool _ICancellingUpdate = false;
        private readonly DpiCorrector _dpiCorrector;
        private readonly SetImage _setImage;
        private bool _dropCondition;
        private bool _dragMoveCondition;

#pragma warning disable CS8618 // Empty class constructor for designer only
        public MainWindowViewModel()
#pragma warning restore CS8618
        {
            if (App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");
        }
        public MainWindowViewModel(IGetImage imageProvider, IWindowPositionUpdater windowPositionUpdater, DpiCorrector dpiCorrector, SetImage setImage)
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
                if (_cancellationToken.IsCancellationRequested is true) throw new InvalidOperationException();
                _windowPositionUpdater.UpdateWindowPos(_cancellationToken.Token);
            }
        }
        private void DropWindowImage(object ev)
        {
            if (BackgroundImage is not ImageSource img) return;
            if (_windowPositionUpdater.IsUpdateWindow is true) return;
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) is not true) return;
            _setImage.SaveImageFromDrop(ev, img);
        }
        private void DragMove() => _windowPositionUpdater.DargMove();
        private void Hide() => VisibleCondition = Visibility.Hidden;
        private void Show() => VisibleCondition = Visibility.Visible;
        private void StopUpdateWindow()
        {
            if (_windowPositionUpdater.IsUpdateWindow is false) return;
            _ICancellingUpdate = true;
            _cancellationToken.Cancel();
            while (System.Threading.SpinWait.SpinUntil(() => _windowPositionUpdater.IsUpdateWindow is false, TimeSpan.FromMilliseconds(300)));
            if (_windowPositionUpdater.IsUpdateWindow is false) throw new InvalidOperationException();
            _cancellationToken = new CancellationTokenSource();
            _ICancellingUpdate = false;
        }
        private async Task SetNewBackgoundImage()
        {
            if (await _imageProvider.GetImageFromClipboard() is not ImageSource image) return;
            DPISacaleMonitor dpi = _dpiCorrector.GetCurretDPI();
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

    public interface IWindowHandler
    {
        public IGetImage ImageProvider { get; }
        public IWindowPositionUpdater PositionUpdater { get; }

    }

    internal class WPFWindowManager : ReactiveObject, IWindowHandler, IWindowPositionUpdater
    {
        private readonly IGetImage _imageProvider;
        private Window _window;
        private IWindowPositionUpdater _positionUpdaterWpf;
        public WPFWindowManager(Window window, IGetImage imageProvider)
        {
            _window = window;
            _imageProvider = imageProvider;
            _positionUpdaterWpf = new Win32WPFWindowPositionUpdater(window);
        }
        public bool IsUpdateWindow => _positionUpdaterWpf.IsUpdateWindow;
        public IGetImage ImageProvider => _imageProvider;
        public IWindowPositionUpdater PositionUpdater => this;

        public void DargMove() => _positionUpdaterWpf.DargMove();
        public Task UpdateWindowPos(CancellationToken token) => RefreshWindow(token);
        private async Task RefreshWindow(CancellationToken token)
        {
            if (_positionUpdaterWpf.IsUpdateWindow is true) return;
            if (_window.Dispatcher.HasShutdownFinished is true) throw new InvalidOperationException("Dispatcher has completed his work");
            await _window.Dispatcher.InvokeAsync(async () => await _positionUpdaterWpf.UpdateWindowPos(token));
        }
    }


    internal class MainWindowCommand : IInvokeShortcuts
    {
        private IKeyboardHandler _keyboardHandler;
        private bool _executePresentNewImage = false;
        private readonly System.Windows.Window _window;
        public MainWindowViewModel MainWindowViewModel { get; }
        public IEnumerable<Shortcuts> GetShortcuts() => new Shortcuts[]
        {
            new Shortcuts(new VKeys[]
            {
                VKeys.VK_LWIN,
                VKeys.VK_SHIFT,
                VKeys.VK_KEY_A
            },
            new Func<Task>(PresentNewImage), nameof(MainWindowCommand.PresentNewImage)),

            new Shortcuts(new VKeys[]
            {
                VKeys.VK_CONTROL,
                VKeys.VK_CAPITAL
            },
            new Func<Task>(SwithBlockRefreshWindow), nameof(MainWindowCommand.SwithBlockRefreshWindow)),

            new Shortcuts(new VKeys[]
            {
                VKeys.VK_CONTROL
            },
            new Func<Task>(StopRefreshWindow), nameof(MainWindowCommand.StopRefreshWindow))

        };
        public MainWindowCommand(System.Windows.Window window, MainWindowViewModel mainWindowView, IKeyboardHandler keyboardHandler)
        {
            _window = window;
            MainWindowViewModel = mainWindowView;
            _keyboardHandler = keyboardHandler;

            IObservable<VKeys[]> keyPressObservable = Observable.FromEventPattern(
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyPressEvent += handler,
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyPressEvent -= handler).Select(x => x.EventArgs.Keys);


            IObservable<VKeys[]> keyUPObservable = Observable.FromEventPattern(
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyUpPressEvent += handler,
                                 (EventHandler<IKeysNotificator> handler) => keyboardHandler.KeyUpPressEvent -= handler).Select(x => x.EventArgs.Keys);

            keyPressObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                if (x.Contains(VKeys.VK_CONTROL) && x.Length is 1)
                {
                    this.MainWindowViewModel.DragMoveCondition = false;
                    MainWindowViewModel.DropCondition = true;
                }              
            });
            keyUPObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x => // todo разобратся почему приходит пустой эвент
            {
                if (this.MainWindowViewModel.VisibleCondition == Visibility.Hidden) return;              
                if (Keyboard.IsKeyUp(Key.LeftCtrl) is true)
                {
                    this.MainWindowViewModel.DragMoveCondition = true;
                    MainWindowViewModel.DropCondition = false;
                }
            });

        }
        public async Task SwithBlockRefreshWindow() => await Application.Current.Dispatcher.InvokeAsync(async () => await MainWindowViewModel.SwithBlockRefreshWindow.Execute().FirstAsync());

        public async Task PresentNewImage()
        {
            if (_executePresentNewImage is true) return;
            try
            {
                _executePresentNewImage = true;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await MainWindowViewModel.SetNewImage.Execute().FirstAsync();
                    await MainWindowViewModel.ShowWindow.Execute().FirstAsync();

                    _ = await this.MainWindowViewModel.RefreshWindowInvoke.CanExecute.FirstAsync() is true ? await MainWindowViewModel.RefreshWindowInvoke.Execute().FirstAsync() : Unit.Default;
                }).Task.ConfigureAwait(false);
            }
            finally { _executePresentNewImage = false; }
        }
        public async Task StopRefreshWindow()
        {
            if (MainWindowViewModel.WindowPositionUpdater.IsUpdateWindow is true)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await MainWindowViewModel.StopWindowUpdater.Execute().FirstAsync());
            }
        }


    }
}
