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

namespace SSHF
{
    
    public partial class App : System.Windows.Application
    {
       // readonly static public System.Windows.Window _GlobalWindowFast = new Func<MainWindow>(() => { if (App.Current.MainWindow is not MainWindow window) throw new NullReferenceException("MainWindow is null?"); return window; }).Invoke();

        readonly static public GlobalLowLevelHooks.KeyboardHook _GlobaKeyboardHook = new GlobalLowLevelHooks.KeyboardHook();

      //  static bool _SingleCopy = default;

        internal static MouseHook mouseHook = new MouseHook();
        internal static readonly DisplayRegistry _displayRegistry = new DisplayRegistry();

        internal static readonly Menu_icon _menu_icon;
      
  

        static App()
        {
            _displayRegistry.RegisterWindowType<MainWindowViewModel, MainWindow>();
            _displayRegistry.RegisterWindowType<NotifyIconViewModel, Menu_icon>();

            if (System.Activator.CreateInstance(_displayRegistry.vmToWindowMapping[typeof(NotifyIconViewModel)]) is not Menu_icon window)
                throw new ArgumentNullException(nameof(window), "Menu_icon");

            _menu_icon = window;

            _WindowInput = new RawInputReceiverWindow();

        }
        public App()
        {
            //Thread threadMouse = new Thread(new ThreadStart(() => { mouseHook.Install(); }));
            //threadMouse.Start();
            // mouseHook.Install();
            //mouseHook.Install();// Почему-то инсталяция в MODEL окна крашит визуальный конструктор
            //try
            //{
            //    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _WindowInput.Handle);
            //    // RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, Visy );
            //    System.Windows.Forms.Application.Run();
            //}
            //finally
            //{
            //    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
            //}
        }

        internal readonly static RawInputReceiverWindow _WindowInput;

    }
        class RawInputReceiverWindow : NativeWindow
        {
            public event EventHandler<RawInputEventArgs>? Input;

            public RawInputReceiverWindow()
            {
                CreateHandle(new CreateParams
                {
                    X = 0,
                    Y = 0,
                    Width = 0,
                    Height = 0,
                    Style = 0x800000,
                });
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_INPUT = 0x00FF;

                if (m.Msg == WM_INPUT)
                {
                    var data = RawInputData.FromHandle(m.LParam);

                    Input?.Invoke(this, new RawInputEventArgs(data));
                }

                base.WndProc(ref m);
            }
        }

        class RawInputEventArgs : EventArgs
        {
            public RawInputEventArgs(RawInputData data)
            {
                Data = data;
            }

            public RawInputData Data { get; }
        }

    
}
