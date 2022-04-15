using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;
using SSHF.Infrastructure.Algorithms;
using System.Windows.Media.Imaging;
using System.IO;

namespace SSHF.Windows
{
    //internal class HandlerFastWindows
    //{
    //    private static readonly bool isInitialize = default;

    //    private readonly object LockObject = new object();

    //    internal Dispatcher GeneralFastDispatcher;


    //    private static readonly List<MainWindow> FastWindows = new List<MainWindow>();

    //    public HandlerFastWindows() : this(new VKeys[] { VKeys.LWIN, VKeys.SHIFT, VKeys.KEY_A })
    //    {
    //    }
    //    public HandlerFastWindows(VKeys[] callbackDelp)
    //    {
    //        GeneralFastDispatcher = Dispatcher.CurrentDispatcher;
    //        if (isInitialize is false) Init(callbackDelp);
    //    }


    //    private void Init(VKeys[] callbackDepl)
    //    {
    //        if (isInitialize is false)
    //        {
    //            _ = Task.Run(() =>
    //            {
    //                KeyboardKeyCallbackFunction callback = KeyboardKeyCallbackFunction.GetInstance();
    //                callback.AddCallBackTask(callbackDepl, () => new Task(async () =>
    //                {
    //                    AlgorithmGetClipboardImage instance = new AlgorithmGetClipboardImage();
    //                    try
    //                    {
    //                        BitmapSource bitSor = await instance.Start<BitmapSource, object>(new object());

    //                        using MemoryStream createFileFromImageBuffer = new MemoryStream();         //todo переехать в интерфейс конвертации изображений
    //                        BitmapEncoder encoder = new PngBitmapEncoder();
    //                        BitmapFrame ccc = BitmapFrame.Create(bitSor);
    //                        encoder.Frames.Add(BitmapFrame.Create(bitSor));
    //                        encoder.Save(createFileFromImageBuffer);
    //                        BitmapImage image = new BitmapImage();
    //                        image.BeginInit();
    //                        image.StreamSource = createFileFromImageBuffer;
    //                        image.EndInit();
    //                        image.Freeze();

    //                        await App.WindowsIsOpen[App.GetMyMainWindow].Value.InvokeAsync(() =>
    //                        {
    //                            _ViewModel.Image = image;
    //                            App.WindowsIsOpen[App.GetMyMainWindow].Key.Height = image.Height;
    //                            App.WindowsIsOpen[App.GetMyMainWindow].Key.Width = image.Width;
    //                            App.WindowsIsOpen[App.GetMyMainWindow].Key.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    //                            App.WindowsIsOpen[App.GetMyMainWindow].Key.Show();
    //                        });
    //                        CancellationTokenSource tokenSource = new CancellationTokenSource();

    //                        void MouseInput(object? sender, Infrastructure.Algorithms.Input.RawInputEvent e)
    //                        {
    //                            if (e.Data is RawInputKeyboardData)
    //                            {
    //                                if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.CONTROL) is true)
    //                                {
    //                                    tokenSource.Cancel();
    //                                    App.Input -= MouseInput;
    //                                    return;
    //                                }
    //                            }
    //                            if (e.Data is not RawInputMouseData data || data.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
    //                            else
    //                            {
    //                                tokenSource.Cancel();
    //                                App.Input -= MouseInput;
    //                            }

    //                        }
    //                        _ = Task.Run(() =>
    //                        {
    //                            App.Input += MouseInput;
    //                        }).ConfigureAwait(false);
    //                        await SSHF.Infrastructure.SharedFunctions.WindowFunctions.RefreshWindowPositin.RefreshWindowPosCursor(App.WindowsIsOpen[App.GetMyMainWindow].Key, tokenSource.Token);
    //                    }
    //                    catch (Exception) { }
    //                })).ConfigureAwait(false);
    //            });
    //        }
    //    }



    //    internal static int CountFastWinows => FastWindows.Count;


    //    internal async Task StartWidnowFast()
    //    {
    //        MainWindow mainWindowNew = new MainWindow();
    //        mainWindowNew.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
    //        mainWindowNew.DataContext = new MainWindowViewModel();
    //        var cursorPosition = CursorFunctions.GetCursorPosition();
    //        mainWindowNew.Left = cursorPosition.X;
    //        mainWindowNew.Top = cursorPosition.Y;
    //        mainWindowNew.Topmost = true;
    //        FastWindows.Add(mainWindowNew);



    //    }

    //}
}
