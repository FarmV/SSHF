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

        private Window? _thisWindow;

        private readonly MainWindowModel _model;
        public MainWindowViewModel()
        {
            _model = new MainWindowModel(this);
        }

        private string _title = "Fast Window";
        public string Title { get => _title; set => Set(ref _title, value); }


        private bool _isRefreshWindow = false;
        public bool RefreshWindow { get => _isRefreshWindow; set => Set(ref _isRefreshWindow, value); }


        private BitmapImage? _ImageBackground;
        public BitmapImage Image
        {
            get => _ImageBackground = _ImageBackground is not null ?
                    _ImageBackground : ImagesFunctions.GetBitmapImage(ImagesFunctions.GetUriApp(@"Windows\MainWindow\MainWindowRes\Test.png")) ?? throw new NullReferenceException();
            set => Set(ref _ImageBackground, value);
        }

        private RelayCommand? _getThisWindow;
        public RelayCommand GetThisWindow => _getThisWindow = _getThisWindow is not null ? _getThisWindow :
                             new RelayCommand(obj => _thisWindow = obj is not RoutedEventArgs ev ?
                              throw new ArgumentException("Неверный входной параметр", typeof(RoutedEventArgs).ToString()) :
                               ev.Source is not Window win ? throw new InvalidOperationException() : win);


        private RelayCommand? _closingCommand;
        public RelayCommand ClosingCommand => _closingCommand = _closingCommand is not null ? _closingCommand : new RelayCommand(obj =>
        {
            _ = obj is not CancelEventArgs eventArgs ? throw new ArgumentException() : eventArgs.Cancel = true;
            _thisWindow?.Hide();
        });


        private RelayCommand? _doubleClickHideWindowCommand;
        public RelayCommand DoubleClickHideWindowCommand => _doubleClickHideWindowCommand = _doubleClickHideWindowCommand is not null ?
                              _doubleClickHideWindowCommand : new RelayCommand(obj => _thisWindow?.Hide());

    }
}
