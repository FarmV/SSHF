using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.ViewModels.Base;
using SSHF.Models.MainWindowModel;
using SSHF.Models;
using System.Windows.Media.Imaging;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.Views.Windows.Notify;
using System.Windows.Interop;
using System.ComponentModel;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel: ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        readonly MainWindowModel _Model;

        public MainWindowViewModel()
        {
            _Model = new MainWindowModel(this);
        }

        #region Заголовок окна
        private string _Title = "Окно быстрого доступа";
        public string Title
        {
            get => _Title; set => Set(ref _Title, value);
        }

        #endregion


        private bool _FlagRefreshCurrentWindow = false;
        public bool RefreshWindow
        {
            get
            {
                return _FlagRefreshCurrentWindow;
            }
            set
            {
                Set(ref _FlagRefreshCurrentWindow, value);
            }
        }


        private BitmapImage? _ImageBackground;
        public BitmapImage Image
        {
            get
            {
                if (_ImageBackground is null) _ImageBackground = ImagesFunctions.SetImageToMemoryFromDrive(ImagesFunctions.GetUriApp(@"Windows\MainWindow\MainWindowRes\Test.png"));
                if (_ImageBackground is null) throw new InvalidOperationException();

                return _ImageBackground;
            }
            set => Set(ref _ImageBackground, value);
        }

                       

        //  private RelayCommand _closingCommand;
        public static RelayCommand ClosingCommand
        {
            get
            {
                return new RelayCommand(obj =>
               {
                   if (obj is not CancelEventArgs e) throw new InvalidOperationException();
                   App.WindowsIsOpen[App.GetMyMainWindow].Key.Hide();
                   e.Cancel = true;
               });

            }
        }
        



    }
}
