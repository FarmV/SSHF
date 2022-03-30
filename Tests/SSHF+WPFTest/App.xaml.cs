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
using SSHF.Views.Windows.NotifyIcon;
using GlobalLowLevelHooks;
using System.Threading;
using Linearstar.Windows.RawInput;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Runtime.CompilerServices;
using SSHF.Infrastructure.SharedFunctions;
using System.Collections.Concurrent;

namespace SSHF
{

    public partial class App: System.Windows.Application
    {

        internal static event EventHandler<SSHF.Infrastructure.Algorithms.Input.RawInputEvent>? Input;

        internal static event EventHandler? DPIChange;

        //  static bool _SingleCopy = default;

        internal static readonly DisplayRegistry RegistartorWindows = new DisplayRegistry();

        internal static bool IsDesignMode { get; private set; } = true;

        internal static int CheckCount = default;

        //  internal static FuncKeyHandler.FkeyHandler? KeyBoardHandler;



        //public static object GetVm([CallerMemberName] string? callMember = null,[CallerFilePath] string? callPath = null)
        // {
        //     string? name2 = callMember;
        //     string? name3 = callPath;

        //     return new object();

        //     if (System.Activator.CreateInstance(RegistartorWindows.vmToWindowMapping[typeof(NotifyIconViewModel)]) is not Menu_icon _menu_icon)
        //         throw new ArgumentNullException(nameof(_menu_icon), "MainWindow");

        // }

        internal volatile static ConcurrentBag<Window> WindowsIsOpen = new ConcurrentBag<Window>();

        internal const string GetWindowNotification = "Нотификация";
        internal const string GetMyMainWindow = "Главное окно приложения";


        async Task StartConfigurations() => await Task.Run(() =>
        {
            Thread[] startInit = new Thread[]
            {
                 new Thread(() =>
                 {
                     Thread.CurrentThread.Name = GetWindowNotification;
                     Menu_icon myNotification = new Menu_icon
                     {
                         Tag = GetWindowNotification,
                         DataContext = new NotifyIconViewModel()
                     };
                     WindowsIsOpen.Add(myNotification);
                     myNotification?.Show();
                     myNotification?.Hide();
                 }),

                 new Thread(() =>
                 {
                      Thread.CurrentThread.Name = GetMyMainWindow;
                      MainWindow mainWindow = new MainWindow
                      {
                          Tag = GetMyMainWindow,
                          DataContext = new MainWindowViewModel()
                      };
                      WindowsIsOpen.Add(mainWindow);
                      mainWindow.Show();
                      mainWindow.Hide();
                      WindowsIsOpen.Add(mainWindow);

                      if (PresentationSource.FromVisual(mainWindow) is not HwndSource source) throw new Exception("Не удалось получить HwndSource окна");
                      source.AddHook(WndProc);
                     
                     
                      RawInputDeviceRegistration[] devices =
                      {
                            new RawInputDeviceRegistration(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle),
                            new RawInputDeviceRegistration(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, source.Handle)
                          };
                      RawInputDevice.RegisterDevice(devices);
                 })
            };
            startInit.AsParallel().ForAll(x =>
            {
                x.SetApartmentState(ApartmentState.STA);
                x.Start();
            });
        });


        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.Name = "General stream!";
            IsDesignMode = false;
            // SystemParameters
            if (IsDesignMode is false)
            {
                Task initializing = StartConfigurations();
            }

            base.OnStartup(e);
        }



        static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;
            const int WM_DPICHANGED = 0x02E0;
            switch (msg)
            {
                case WM_INPUT:
                    {
                        Task.Factory.StartNew(() =>
                        {
                            RawInputData? data = RawInputData.FromHandle(lParam);
                            Input?.Invoke(App.Current.MainWindow, new SSHF.Infrastructure.Algorithms.Input.RawInputEvent(data));
                        }).Start();
                    }
                    break;
                case WM_DPICHANGED:
                    {
                        Task.Factory.StartNew(() => { DPIChange?.Invoke(new object(), EventArgs.Empty); }).Start();
                    }
                    break;
            }
            return hwnd;
        }



    }
}
