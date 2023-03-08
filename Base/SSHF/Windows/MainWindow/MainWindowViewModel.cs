using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF.Infrastructure;
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
using System.Threading;
using System.Windows.Media;
using System.Reflection;
using ReactiveUI;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel : ReactiveObject
    {
        private bool _isRefreshWindow = false;
        private BitmapImage? _imageBackground;
        private bool _blockRefresh;
        private RelayCommand? _getThisWindow;
        private RelayCommand? _closingCommand;
        private RelayCommand? _doubleClickHideWindowCommand;
        private RelayCommand? _mouseWheel;
        ~MainWindowViewModel() => ClearTMP();     
        public MainWindowViewModel()
        {
           // if (Program.DesignerMode is not true) throw new InvalidOperationException("Пустой конструтор только для дизайнера");            
        }
        private static Uri GetUriApp(string resourcePath) => new Uri(string.Format("pack://application:,,,/{0};component/{1}", Assembly.GetExecutingAssembly().GetName().Name, resourcePath));
        public bool IsRefreshWindow 
        { 
            get => _isRefreshWindow; 
            set => this.RaiseAndSetIfChanged(ref _isRefreshWindow, value); 
        }
        public bool BlockRefresh
        {
            get { return _blockRefresh; }
            set => this.RaiseAndSetIfChanged(ref _blockRefresh, value);
        }
        public BitmapImage BackgroundImage
        {
            get => _imageBackground = _imageBackground is not null ? _imageBackground : Images.GetBitmapImage(GetUriApp(@"Windows\MainWindow\MainWindowRes\Test.png")) ?? throw new NullReferenceException();
            set => this.RaiseAndSetIfChanged(ref _imageBackground, value);
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
