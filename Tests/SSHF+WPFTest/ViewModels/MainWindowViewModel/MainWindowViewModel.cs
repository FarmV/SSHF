using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF_WPFTest.Infrastructure;
using SSHF_WPFTest.ViewModels.Base;
using SSHF_WPFTest.Models.MainWindowModel;

namespace SSHF_WPFTest.ViewModels
{
    internal partial class MainWindowViewModel : ViewModel
    {
        #region Заголовок окна

        private string _Title = "Окно быстрого доступа";
        /// <summary>Заголовок окна</summary>
        public string Title { get => _Title; set => Set(ref _Title, value); }
        
        #endregion






    }
}
