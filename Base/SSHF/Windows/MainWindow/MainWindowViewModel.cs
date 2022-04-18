using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.ViewModels.Base;
using SSHF.Models.MainWindowModel;
using SSHF.Models;
using System.Windows.Media.Imaging;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.Views.Windows.Notify;
using System.Windows.Interop;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.IO;
using Linearstar.Windows.RawInput;
using SSHF.Infrastructure.Algorithms.KeyBoards.Base;
using System.Threading;
using System.Windows.Media;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel : ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        private Window? _thisWindow;

        private readonly MainWindowModel _model;
        public MainWindowViewModel() { _model = new MainWindowModel(this); }

        private string _title = "Fast Window";
        public string Title { get => _title; set => Set(ref _title, value); }






        private bool _isRefreshWindow = false;
        public bool IsRefreshWindow { get => _isRefreshWindow; set => Set(ref _isRefreshWindow, value); }


        private BitmapImage? _ImageBackground;
        public BitmapImage BackgroundImage
        {
            get => _ImageBackground = _ImageBackground is not null ?
                    _ImageBackground : ImagesFunctions.GetBitmapImage(ImagesFunctions.GetUriApp(@"Windows\MainWindow\MainWindowRes\Test.png")) ?? throw new NullReferenceException();
            set => Set(ref _ImageBackground, value);
        }

        private RelayCommand? _getThisWindow;
        public RelayCommand GetThisWindow => _getThisWindow = _getThisWindow is not null ? _getThisWindow :
                             new RelayCommand(obj => _thisWindow = obj is not RoutedEventArgs ev ?
                              throw new ArgumentException("Неверный входной параметр", typeof(RoutedEventArgs).ToString()) :
                               ev.Source is not Window win ? throw new InvalidOperationException() : win);


        private RelayCommand? _closingCommand;
        public RelayCommand NotClosingCommand => _closingCommand = _closingCommand is not null ? _closingCommand : new RelayCommand(obj =>
        {
            _ = obj is not CancelEventArgs eventArgs ? throw new ArgumentException() : eventArgs.Cancel = true;
            _thisWindow?.Hide();
        });


        private RelayCommand? _doubleClickHideWindowCommand;
        public RelayCommand DoubleClickHideWindowCommand => _doubleClickHideWindowCommand = _doubleClickHideWindowCommand is not null ?
                             _doubleClickHideWindowCommand : new RelayCommand(obj => _thisWindow?.Hide());




        private RelayCommand? _mouseWheel;
        public RelayCommand MouseWheel => _mouseWheel = _mouseWheel is not null ? _mouseWheel : new RelayCommand(obj =>
        {
            int res = obj is not MouseWheelEventArgs eventArgs ? throw new ArgumentException() : eventArgs.Delta;

            if (res > 0)
            {
                if (_thisWindow is null) throw new NullReferenceException(nameof(_thisWindow));

                _thisWindow.Width += 20;
                _thisWindow.Height += 20;

                eventArgs.Handled = true;
                return;
            }
            if (res < 0)
            {
                if (_thisWindow is null) throw new NullReferenceException(nameof(_thisWindow));
                if (_thisWindow.Width - 20 < 0 || _thisWindow.Height - 20 < 0) return;
                _thisWindow.Width -= 20;
                _thisWindow.Height -= 20;
                eventArgs.Handled = true;
                return;
            }

        });


        ~MainWindowViewModel()
        {
            foreach (var deleteFile in deleteTMP)
            {
                if (File.Exists(deleteFile)) File.Delete(deleteFile);
            }

        }


        private static readonly List<string> deleteTMP = new List<string>();
        private RelayCommand? _dropImage;
        public RelayCommand DropImage => _dropImage = _dropImage is not null ? _dropImage : new RelayCommand(obj =>
        {
            if (IsRefreshWindow is true) return;
            if (obj is not MouseEventArgs Event) throw new InvalidOperationException();

            if (( Event.MouseDevice.LeftButton == MouseButtonState.Pressed ) & KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL) is true)
            {
                string temp = Path.GetTempFileName();
                FileInfo fileInfo = new FileInfo(temp)
                {
                    Attributes = FileAttributes.Temporary
                };
                temp = Path.ChangeExtension(Path.GetTempFileName(), "png");
                using (FileStream createFile = new FileStream(temp, FileMode.OpenOrCreate))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_ImageBackground));
                    encoder.Save(createFile);

                }

                string[] arrayDrops = new string[] { temp };
                DataObject dataObject = new DataObject(DataFormats.FileDrop, arrayDrops);
                dataObject.SetData(DataFormats.StringFormat, dataObject);

                if (IsRefreshWindow is true) return;
                DragDrop.DoDragDrop((System.Windows.Controls.Border)Event.Source, dataObject, DragDropEffects.Copy);

                Array.ForEach(deleteTMP.ToArray(), (x) => { if (File.Exists(x) is true) File.Delete(x); else deleteTMP.Remove(x); });
                deleteTMP.Add(temp);
            }

        });

        private bool _blockRefresh;

        public bool BlockRefresh
        {
            get { return _blockRefresh; }
            set => Set(ref _blockRefresh, value);
        }




        private RelayCommand? _refreshWindow;
        public RelayCommand InvoceRefreshWindow => _refreshWindow = _refreshWindow is not null ? _refreshWindow : new RelayCommand(async obj =>
        {
            if (_thisWindow is null) throw new NullReferenceException();
            await _thisWindow.Dispatcher.InvokeAsync(async () =>
            {
                if (IsRefreshWindow is true) return;
                if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL)) return;

                try
                {
                    if (System.Windows.Clipboard.ContainsImage() is true)
                    {
                        BitmapSource bitSor = System.Windows.Clipboard.GetImage();
                        RenderOptions.SetBitmapScalingMode(bitSor, BitmapScalingMode.NearestNeighbor);

                        using MemoryStream createFileFromImageBuffer = new MemoryStream();         //todo переехать в интерфейс конвертации изображений
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        BitmapFrame ccc = BitmapFrame.Create(bitSor);
                        encoder.Frames.Add(BitmapFrame.Create(bitSor));
                        encoder.Save(createFileFromImageBuffer);
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = createFileFromImageBuffer;
                        image.EndInit();

                        var curPos = SSHF.Infrastructure.SharedFunctions.CursorFunctions.GetCursorPosition();

                        BackgroundImage = image;
                        _thisWindow.Height = image.Height;
                        _thisWindow.Width = image.Width;
                        if (BlockRefresh is not true)
                        {
                            _thisWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                            _thisWindow.Top = curPos.Y;
                            _thisWindow.Left = curPos.X;
                        }
                        _thisWindow.Show();
                        _thisWindow.Focus();
                    } else
                    {
                        _thisWindow.Show();
                        _thisWindow.Focus();
                    }

                    CancellationTokenSource tokenSource = new CancellationTokenSource();

                    void MouseInput(object? sender, Infrastructure.Algorithms.Input.RawInputEvent e)
                    {
                        if (e.Data is RawInputKeyboardData)
                        {
                            if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL) is true)
                            {
                                tokenSource.Cancel();
                                App.Input -= MouseInput;
                                return;
                            }
                        }
                        if (e.Data is not RawInputMouseData data || data.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
                        else
                        {
                            tokenSource.Cancel();
                            App.Input -= MouseInput;
                        }

                    }
                    _ = Task.Run(() =>
                    {
                        App.Input += MouseInput;
                    }).ConfigureAwait(false);

                    if (BlockRefresh is true) return;
                    IsRefreshWindow = true;
                    await _thisWindow.Dispatcher.InvokeAsync(new Action(async () => { await WindowFunctions.RefreshWindowPositin.RefreshWindowPosCursor(App.WindowsIsOpen[App.GetMyMainWindow].Key, tokenSource.Token); }),
                        System.Windows.Threading.DispatcherPriority.Send);

                    IsRefreshWindow = false;

                } catch (Exception) { }
            }, System.Windows.Threading.DispatcherPriority.Send);



        });


    }
}
