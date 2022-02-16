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
using SSHF.Views.Windows.NotifyIcon;
using System.Windows.Interop;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel : ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        readonly MainWindowModel _Model;
        readonly CursorFunction _Cursor;

        public MainWindowViewModel()
        {
            _Model = new MainWindowModel(this);
            _Cursor = new CursorFunction();
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



        private BitmapImage? _ImageForButton;

        public BitmapImage Image
        {
            get
            {
                if (_ImageForButton is null) _ImageForButton = IntegratingImages.SetImageToMemoryFromDrive(IntegratingImages.GetUriApp("Views/Windows/MainWindow/MainWindowRes/F_Logo2.png"));
                if (_ImageForButton is null) throw new InvalidOperationException();

                return _ImageForButton;
            }
            set => Set(ref _ImageForButton, value);
        }


        private BitmapImage? _ImageForButtonOpacity;

        public BitmapImage? ImageOpacity
        {
            get
            {
                if (_ImageForButtonOpacity is null) return Image;
               // if (_ImageForButtonOpacity is null) throw new InvalidOperationException();

                return _ImageForButtonOpacity;
            }
            set
            {
                if (value is null) return;
                _ImageForButtonOpacity = IntegratingImages.ImageScale(value);
                Set(ref _ImageForButtonOpacity, value); 
            }
        }



    }
}
