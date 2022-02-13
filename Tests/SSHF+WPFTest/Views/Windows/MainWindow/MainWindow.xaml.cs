using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

namespace SSHF
{



    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

        }
     
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //if (PresentationSource.FromVisual(this) is not HwndSource source) throw new Exception("Не удалось получить HwndSource окна");
            //source.AddHook(WndProc);
            //RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, source.Handle);
        }

        
        //IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    const int WM_INPUT = 0x00FF;
        //    switch (msg)
        //    {
        //        case WM_INPUT:
        //            {
        //                System.Diagnostics.Debug.WriteLine("Received WndProc.WM_INPUT");
        //                RawInputData? data = RawInputData.FromHandle(lParam);

        //                App.SetRawData(data);                                            
        //            }
        //            break;
        //    }
        //    return hwnd;
        //}
    }
}
