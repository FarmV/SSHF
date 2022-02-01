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

namespace SSHF
{
    
    public partial class App : Application
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


        }
        public App()
        {

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // _displayRegistry.ShowPresentation(_NotifyIconViewModel as object);
            //_displayRegistry.HidePresentation(_NotifyIconViewModel as object);

            //await _displayRegistry.ShowModalPresentation(new NotifyIconViewModel() as object);


        }
    }
}
