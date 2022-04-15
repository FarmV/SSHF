using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels;
using SSHF.Infrastructure;

using System.Windows.Forms;

using static SSHF.Infrastructure.SharedFunctions.CursorFunctions;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Models.NotifyIconModel;
using System.Collections.Generic;
using SSHF.ViewModels.NotifyIconViewModel;
using System.Linq;
using Linearstar.Windows.RawInput;
using SSHF.Infrastructure.Algorithms;
using SSHF.Infrastructure.Interfaces;
using SSHF.Infrastructure.Algorithms.Base;
using System.Threading;
using SSHF.Infrastructure.Algorithms.KeyBoards.Base;
using System.ComponentModel;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            _ViewModel = ViewModel;

            if (App.IsDesignMode is not true)
            {
               
            }
          
        }
        

        // todo перенести в нужную модель
        #region Выбор файла изображения

        public void SelectFileExecute(object? parameter)
        {
            if (DialogFileFunctions.OpenFile("Выбор изображения", out string? filePath) is false) return;

            if (filePath is null) return;

            FileInfo file = new FileInfo(filePath);

            BitmapImage? image = ImagesFunctions.GetBitmapImage(new Uri(file.FullName, UriKind.Absolute));

            if (image is null)
            {
                System.Windows.Forms.MessageBox.Show($"Не удалось обработать файл изображения.{Environment.NewLine}Проверте расширение файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };

            _ViewModel.BackgroundImage = image;
        }        
        #endregion
        
        
      
    }

}
