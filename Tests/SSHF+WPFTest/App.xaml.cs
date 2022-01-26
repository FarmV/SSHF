using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using SSHF.ViewModels.NotifyIconViewModel;

namespace SSHF
{
    
    public partial class App : Application
    {
        readonly static public System.Windows.Window _GlobalWindowFast = new Func<MainWindow>(() => { if (App.Current.MainWindow is not MainWindow window) throw new NullReferenceException("MainWindow is null?"); return window; }).Invoke();

        readonly static public GlobalLowLevelHooks.KeyboardHook _GlobaKeyboardHook = new GlobalLowLevelHooks.KeyboardHook();

        static bool _SingleCopy = default;

        readonly NotifyIconViewModel _NotifyIconViewModel = new NotifyIconViewModel();


       // public DisplayRootRegistry displayRootRegistry = new DisplayRootRegistry();


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            

            //Shutdown( )
        }
    }
}
