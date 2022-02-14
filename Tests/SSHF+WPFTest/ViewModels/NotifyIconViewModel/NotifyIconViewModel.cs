using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;
using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotifyIconViewModel : ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        

        readonly NotifyIconModel _model;
        public NotifyIconViewModel()
        {          
           _model = new NotifyIconModel(this);
           
        }

        bool _visible = default;    

        public bool MyProperty
        {
            get { return _visible; }
            set { _visible = value; }
        }

        public ICommand ShutdownAppCommand => new RelayCommand(_model.ShutdownAppExecute, _model.IsExecuteShutdownApp);



    }
}
