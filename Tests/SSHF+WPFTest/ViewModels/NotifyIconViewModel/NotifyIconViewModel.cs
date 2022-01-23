using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;
using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotifyIconViewModel : ViewModel
    {



        NotifyIconModel _Model;
        public NotifyIconViewModel()
        {
            _Model = new NotifyIconModel();

        }

        private Menu_icon? _window;

        public Menu_icon? Menu_iconWindow
        {
            get { return _window; }
            init
            {

            }
        }
    }
}
