using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using SSHF.Infrastructure;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Views.Windows.Notify;
using System.Threading;
using Linearstar.Windows.RawInput;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Runtime.CompilerServices;
using SSHF.Infrastructure.SharedFunctions;
using System.Collections.Concurrent;
using System.Windows.Threading;
using SSHF.ViewModels;
using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Algorithms;
using System.Windows.Media.Imaging;
using System.IO;
using SSHF.Infrastructure.Algorithms.KeyBoards.Base;
using SSHF.ViewModels.Base;
using System.Windows.Media;

namespace SSHF
{

    public partial class App : System.Windows.Application, IAsyncDisposable
    {
        protected override async void OnExit(ExitEventArgs e) => await DisposeAsync();

        public async ValueTask DisposeAsync()
        {
            if (CallbackFunction is not null) await CallbackFunction.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        internal static KeyboardKeyCallbackFunction? CallbackFunction;

        internal static bool IsDesignMode { get; private set; } = true;

        internal static event EventHandler<SSHF.Infrastructure.Algorithms.Input.RawInputEvent>? Input;

        internal static event EventHandler? DPIChange;

        internal const string GetMyMainWindow = "Главное окно приложения - поток";
        internal const string GetWindowNotification = "Нотификация окно - поток";
        internal volatile static ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>> WindowsIsOpen = new ConcurrentDictionary<string, KeyValuePair<Window, Dispatcher>>();

        private static NotificatorViewModel? Notificator;
        internal static NotificatorViewModel GetNotificator() => Notificator is null ? throw new NullReferenceException("Нотификатор не инициализирован") : Notificator;

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.Name = "General stream!";
            IsDesignMode = false;
            // SystemParameters
            if (IsDesignMode is false)
            {
                _ = StartConfigurations();

            }
            base.OnStartup(e);
        }

        static private Task StartConfigurations() => _ = Task.Run(() =>
        {
            Thread[] startInit = new Thread[]
            {
                 new Thread(() =>
                 {
                      Dispatcher dispThreadMainWindow = Dispatcher.CurrentDispatcher;
                      Thread.CurrentThread.Name = GetMyMainWindow;
                      MainWindow mainWindow = new MainWindow
                      {
                          Tag = GetMyMainWindow,
                          DataContext = new MainWindowViewModel()
                      };
                      mainWindow.Show();
                      mainWindow.Hide();
                      if(WindowsIsOpen.TryAdd(GetMyMainWindow, new KeyValuePair<Window,Dispatcher>(mainWindow, dispThreadMainWindow)) is not true) throw new InvalidOperationException();
                      System.Windows.Threading.Dispatcher.Run();
                 }),
                 new Thread(async () =>
                 {
                     Dispatcher dispThreadNotification = Dispatcher.CurrentDispatcher;

                     Thread.CurrentThread.Name = GetWindowNotification;
                     Notificator myNotification = new Notificator
                     {
                         Tag = GetWindowNotification,
                     };
                     NotificatorViewModel noti = new NotificatorViewModel();
                     Notificator = noti;
                     myNotification.DataContext = noti;

                     myNotification.Show();
                     myNotification.Hide();

                     await using TrayIcon icon = new TrayIcon();

                     if(WindowsIsOpen.TryAdd(GetWindowNotification, new KeyValuePair<Window,Dispatcher>(myNotification, dispThreadNotification)) is not true) throw new InvalidOperationException();

                     System.Windows.Threading.Dispatcher.Run();
                 })
            };

            _ = Task.Run(() =>
            {
                startInit.AsParallel().ForAll(x =>
                {
                    x.SetApartmentState(ApartmentState.STA);
                    x.Start();
                });

                while (WindowsIsOpen.Count is not 2) { }
            }).ContinueWith(async t =>
            {
                Dispatcher dispThreadMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;
                await dispThreadMainWindow.InvokeAsync(() =>
                {
                    Thread dispMain = dispThreadMainWindow.Thread;
                    dispMain.Name = GetMyMainWindow;
                    Dispatcher disMainWindow = WindowsIsOpen[GetMyMainWindow].Key.Dispatcher;

                    Window mainWindow = WindowsIsOpen[GetMyMainWindow].Key;

                    if (WindowsIsOpen.TryUpdate(GetMyMainWindow, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetMyMainWindow]) is not true) throw new InvalidOperationException();
                }, DispatcherPriority.Render);

                _ = WindowsIsOpen[GetMyMainWindow].Value.InvokeAsync(() =>
                {
                    if (PresentationSource.FromVisual(WindowsIsOpen[GetMyMainWindow].Key) is not HwndSource source) throw new NullReferenceException("Не удалось получить HwndSource окна");

                    RawInputDeviceRegistration[] devices =
                    {
                             new RawInputDeviceRegistration(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle),
                             new RawInputDeviceRegistration(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, source.Handle)
                    };

                    RawInputDevice.RegisterDevice(devices);
                    source.AddHook(WndProc);

                    static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        const int WM_INPUT = 0x00FF;
                        const int WM_DPICHANGED = 0x02E0;

                        switch (msg)
                        {
                            case WM_INPUT:
                            {


                                RawInputData? data = RawInputData.FromHandle(lParam); // нельзя асинхронно
                                Input?.Invoke(WindowsIsOpen[GetMyMainWindow].Key, new Infrastructure.Algorithms.Input.RawInputEvent(data));
                            }
                            break;
                            case WM_DPICHANGED:
                            {
                                _ = Task.Run(() => { DPIChange?.Invoke(new object(), EventArgs.Empty); }).ConfigureAwait(false);
                            }
                            break;
                        }
                        return hwnd;
                    }
                }, DispatcherPriority.Render);

            }).ContinueWith((taskInTask) =>
            {
                _ = Task.Run(() => { CallbackFunction = KeyboardKeyCallbackFunction.GetInstance(); });


                Dispatcher dispThreadMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

                _ = dispThreadMainWindow.InvokeAsync(() =>
                {
                    Thread dispMain = dispThreadMainWindow.Thread;
                    dispMain.Name = GetWindowNotification;
                    Dispatcher disMainWindow = WindowsIsOpen[GetWindowNotification].Key.Dispatcher;

                    Window mainWindow = WindowsIsOpen[GetWindowNotification].Key;

                    if (WindowsIsOpen.TryUpdate(GetWindowNotification, new KeyValuePair<Window, Dispatcher>(mainWindow, disMainWindow), WindowsIsOpen[GetWindowNotification]) is not true) throw new InvalidOperationException();
                }, DispatcherPriority.Render);
            }).ContinueWith((T) =>
            {
                // Регстрация функций
                //Depl translete

                KeyboardKeyCallbackFunction callback = KeyboardKeyCallbackFunction.GetInstance();
                string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";
                string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";
                var keyCombianteionGetTranslate = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
                {
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_1,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_2,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_3
                };
                callback.AddCallBackTask(keyCombianteionGetTranslate, () => new Task(async () =>
                {
                    AlgorithmGetTranslateAbToDepl instance = await AlgorithmGetTranslateAbToDepl.GetInstance(DeeplDirectory, ScreenshotReaderDirectory);
                    try
                    {
                        await instance.Start<string, bool>(false);
                    } catch (Exception) { }
                })).ConfigureAwait(false);


                // показ окна Fast
                var keyCombianteionGetClipboardImageAndRefresh = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
                {
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_LWIN,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_SHIFT,
                 Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_KEY_A
                };
                callback.AddCallBackTask(keyCombianteionGetClipboardImageAndRefresh, () => new Task(() =>
                {
                   _ = WindowsIsOpen[GetMyMainWindow].Value.InvokeAsync(() => ((System.Windows.Input.ICommand)(( (MainWindowViewModel)WindowsIsOpen[GetMyMainWindow].Key.DataContext ).InvoceRefreshWindow)).Execute(new object()));          
                    
                })).ConfigureAwait(false);
                // блок обновления окна
                var keyCombianteionBlockWindowRefreshFast = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
                {
                  Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CONTROL,
                  Infrastructure.Algorithms.Input.Keybord.Base.VKeys.VK_CAPITAL,

                };
                callback.AddCallBackTask(keyCombianteionBlockWindowRefreshFast, () => new Task(async () =>
                {
                    await App.WindowsIsOpen[App.GetMyMainWindow].Value.InvokeAsync(() =>
                    {
                        MainWindowViewModel ViewModel = (MainWindowViewModel)WindowsIsOpen[GetMyMainWindow].Key.DataContext;
                        if (ViewModel.IsRefreshWindow is true) return;
                        ViewModel.BlockRefresh = !ViewModel.BlockRefresh;
                    });

                })).ConfigureAwait(false);


            });
        });

    }
}
