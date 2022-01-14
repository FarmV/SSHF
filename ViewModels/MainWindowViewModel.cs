﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF_.ViewModels.Base;

namespace SSHF_.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Заголовок окна
        private string _Title = "Агрегация закладок";
        /// <summary>Заголовок окна</summary>
        public string Title
        {
            get => _Title; 
            set => Set(ref _Title, value);
        }
        #endregion

    }
}