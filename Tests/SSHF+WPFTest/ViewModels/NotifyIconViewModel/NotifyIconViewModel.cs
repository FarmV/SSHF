using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotifyIconViewModel : ViewModel
    {

        NotifyIconModel _Model;
        public NotifyIconViewModel()
        {
            _Model = new NotifyIconModel();
        }
    }
}
