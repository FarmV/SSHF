using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.MainWindowModel;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel
    {
        RelayCommand? _RCom;

        public ICommand RefreshOnCommand => new RelayCommand(_Model.RefreshWindowOnExecute, _Model.IsRefreshWindowOn);

        public ICommand RefreshOffCommand => new RelayCommand(_Model.RefreshWindowOffExecute, _Model.IsExecuteRefreshWindowOff);



        public ICommand SelectFileCommand => new RelayCommand(_Model.SelectFileExecute, _Model.IsExecuteSelectFile);
        
        public ICommand ApplicationShutdown => new RelayCommand((obj) => Application.Current.Shutdown(), (obj) => true);



        public ICommand IvoceNotificatorView => new RelayCommand(_Model.NotificatorExecute, _Model.IsExecuteInvoceNotificator);


        public ICommand InvoceSaveFileDialogCommand => new RelayCommand(_Model.SaveFileDialogExecute, _Model.IsExecuteSaveFileDialog);






    }

}
