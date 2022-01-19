using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.MainWindowModel;

namespace SSHF.ViewModels
{
    internal partial class MainWindowViewModel
    {
        RelayCommand? _RCom;
        public ICommand RefreshON => new RelayCommand(MainWindowModel.ExecuteRefreshWindowOn, MainWindowModel.CanExecuteRefreshWindowOn);

        public ICommand RefreshOFF => new RelayCommand(MainWindowModel.CommandExecuteRefreshWindowOFF, MainWindowModel.CanCommandExecuteRefreshWindowOFF);

    }

}
