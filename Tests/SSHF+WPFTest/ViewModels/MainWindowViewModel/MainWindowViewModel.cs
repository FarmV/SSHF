using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.ViewModels.Base;
using SSHF.Models.MainWindowModel;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel : ViewModel
    {
        readonly MainWindowModel _Model;
       
        
        public MainWindowViewModel()
        {
            _Model = new MainWindowModel(this);
        }
        #region Заголовок окна

        private string _Title = "Окно быстрого доступа";
        /// <summary>Заголовок окна</summary>
        public string Title { get => _Title; set => Set(ref _Title, value); }

        #endregion

        bool _FlagRefreshCurrentWindow = false;


        public bool RefreshWindow
        {
            get { return _FlagRefreshCurrentWindow; }
            set { Set(ref _FlagRefreshCurrentWindow, value); }


        }
    }}
