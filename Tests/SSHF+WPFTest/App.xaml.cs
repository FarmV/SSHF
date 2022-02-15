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

namespace SSHF
{

    public partial class App : System.Windows.Application
    {
        internal static event EventHandler<RawInputEventArgs>? Input;
       
        internal static event EventHandler? DPIChange;

        readonly static public GlobalLowLevelHooks.KeyboardHook _GlobaKeyboardHook = new GlobalLowLevelHooks.KeyboardHook();

        //  static bool _SingleCopy = default;

        internal static readonly DisplayRegistry RegistartorWindows = new DisplayRegistry();

        internal static bool IsDesignMode { get; private set; } = true;

        internal static int CheckCount = default;

       //public static object GetVm([CallerMemberName] string? callMember = null,[CallerFilePath] string? callPath = null)
       // {
       //     string? name2 = callMember;
       //     string? name3 = callPath;

       //     return new object();

       //     if (System.Activator.CreateInstance(RegistartorWindows.vmToWindowMapping[typeof(NotifyIconViewModel)]) is not Menu_icon _menu_icon)
       //         throw new ArgumentNullException(nameof(_menu_icon), "MainWindow");

       // }

        protected override void OnStartup(StartupEventArgs e)
        {
            IsDesignMode = false;

            if (IsDesignMode is false)
            {
                


                RegistartorWindows.RegisterWindowType<NotifyIconViewModel, Menu_icon>();
                RegistartorWindows.RegisterWindowType<MainWindowViewModel, MainWindow>();



                MainWindowViewModel main = new MainWindowViewModel();
                RegistartorWindows.PresentationON(main);

                

                NotifyIconViewModel noti = new NotifyIconViewModel();
                RegistartorWindows.PresentationON(noti);

                RegistartorWindows.HideView(noti);



                //if (System.Activator.CreateInstance(RegistartorWindows.vmToWindowMapping[typeof(NotifyIconViewModel)]) is not Menu_icon _menu_icon)
                //    throw new ArgumentNullException(nameof(_mainWindow), "MainWindow");


                Window? mainWindow = RegistartorWindows.GetWindow(main);
                if (mainWindow is null) throw new Exception("NULL");
                
                
                
               // mainWindow.Hide(); // тут олдака


                if (PresentationSource.FromVisual(mainWindow) is not HwndSource source) throw new Exception("Не удалось получить HwndSource окна");
                source.AddHook(WndProc);
                RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle);
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
                        System.Diagnostics.Debug.WriteLine("Received WndProc.WM_INPUT");
                        RawInputData? data = RawInputData.FromHandle(lParam);

                        Input?.Invoke(App.Current.MainWindow, new RawInputEventArgs(data));
                    }
                    break;
                case WM_DPICHANGED:
                    {
                        DPIChange?.Invoke(new object(),EventArgs.Empty);
                    }
                    break;
            }
            return hwnd;
        }



    }

    internal class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(RawInputData data)
        {
            Data = data;
        }

        public RawInputData Data { get; }
    }

    //class RawInputReceiverWindow : NativeWindow
    //{
    //    public event EventHandler<RawInputEventArgs>? Input;

    //    public RawInputReceiverWindow()
    //    {
    //        CreateHandle(new CreateParams
    //        {
    //            X = 0,
    //            Y = 0,
    //            Width = 0,
    //            Height = 0,
    //            Style = 0x800000,
    //        });
    //    }

    //    protected override void WndProc(ref Message m)
    //    {
    //        const int WM_INPUT = 0x00FF;

    //        if (m.Msg == WM_INPUT)
    //        {
    //            var data = RawInputData.FromHandle(m.LParam);

    //            Input?.Invoke(this, new RawInputEventArgs(data));
    //        }

    //        base.WndProc(ref m);
    //    }
    //}



}
