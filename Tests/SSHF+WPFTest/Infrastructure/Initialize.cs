using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.ViewModels.NotifyIconViewModel;

namespace SSHF.Infrastructure
{
    internal class Initialize
    {
        

        readonly static public System.Windows.Window _GlobalWindowFast = App.Current.MainWindow;

        readonly static public GlobalLowLevelHooks.KeyboardHook _GlobaKeyboardHook = new GlobalLowLevelHooks.KeyboardHook();

        static bool _SingleCopy = default;

        readonly NotifyIconViewModel _NotifyIconViewModel;

      public  Initialize()
      {
            if (_SingleCopy is true) throw new InvalidOperationException("Попытка создать более 1 копии экземляра класса инициализации");
            _SingleCopy = true;

            _NotifyIconViewModel = new NotifyIconViewModel();

            //_NotifyIconViewModel = new NotifyIconViewModel();
            // App.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        }
    }
}
