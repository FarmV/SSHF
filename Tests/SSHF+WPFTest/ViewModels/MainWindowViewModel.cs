using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF_WPFTest.Infrastructure;
using SSHF_WPFTest.ViewModels.Base;

namespace SSHF_WPFTest.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Заголовок окна
        private string _Title = "Окно быстрого доступа";
        /// <summary>Заголовок окна</summary>
        public string Title
        {
            get => _Title; 
            set => Set(ref _Title, value);
        }
        #endregion
        RelayCommand? TestCommand;
        public ICommand MyCommand
        {
            get
            {
                if (TestCommand == null)
                    TestCommand = new RelayCommand(CommandExecute, CanCommandExecute);
                return TestCommand;
            }
        }
        private void CommandExecute(object parameter)
        {
            //MessageBox.Show("Привет " + Convert.ToString(parameter));
        }

        private bool CanCommandExecute(object parameter)
        {
            return true;
            //return TextBox1.Text != string.Empty;
        }

    }
}
