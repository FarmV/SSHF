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
    internal partial class MainWindowViewModel : ViewModel
    {



        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        private readonly MainWindowModel _model;

        public MainWindowViewModel()
        {
            _model = new MainWindowModel(this);
     
        }   

        private string _title = "Fast Window";
        public string Title { get => _title; set => Set(ref _title, value); }


        private bool _isRefreshWindow = false;
        public bool RefreshWindow { get => _isRefreshWindow; set { Set(ref _isRefreshWindow, value); } }


        private BitmapImage? _ImageBackground;
        public BitmapImage Image
        {
            get => _ImageBackground = _ImageBackground is not null ?
                    _ImageBackground : ImagesFunctions.GetBitmapImage(ImagesFunctions.GetUriApp(@"Windows\MainWindow\MainWindowRes\Test.png")) ?? throw new NullReferenceException();
            set => Set(ref _ImageBackground, value);
        }


        private static RelayCommand? _closingCommand;
        public static RelayCommand ClosingCommand
        {
            get => _closingCommand = _closingCommand is not null ? _closingCommand : new RelayCommand(obj =>
            {
              //  _ = obj is not CancelEventArgs eventArgs ? throw new ArgumentException()
              //  eventArgs.

                if (obj is not CancelEventArgs evArgs) throw new InvalidOperationException();
                App.WindowsIsOpen[App.GetMyMainWindow].Key.Hide();
                evArgs.Cancel = true;
            });
        }

        private  RelayCommand? _doubleClickHideWindowCommand;
        public  RelayCommand DoubleClickHideWindowCommand
        {
            get => new RelayCommand((_) => _window1.Hide() );
            
        }


        private Window _window1;
        private  RelayCommand? _getThisWindow;
        public RelayCommand GetThisWindow
        {
            get => _getThisWindow = _getThisWindow is not null ? _getThisWindow :
                        new RelayCommand(obj => _window1 = obj is not RoutedEventArgs ev ?
                         throw new ArgumentException("Неверный входной параметр", typeof(RoutedEventArgs).ToString()) :
                          ev.Source is not Window win ? throw new InvalidOperationException() : win);            
        }
    }
}
