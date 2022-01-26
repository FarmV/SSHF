using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        bool _visible = default;    

        public bool MyProperty
        {
            get { return _visible; }
            set { _visible = value; }
        }



    }
}
