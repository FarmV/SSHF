using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.MainWindowModel;

namespace SSHF.ViewModels.MainWindowViewModel
{
    internal partial class MainWindowViewModel
    {
        RelayCommand? _RCom;

        public ICommand RefreshON => new RelayCommand(_Model.RefreshWindowOnExecute, _Model.IsRefreshWindowOn);

        public ICommand RefreshOFF => new RelayCommand(_Model.RefreshWindowOffExecute, _Model.IsExecuteRefreshWindowOff);

       // public ICommand HelpMenu => new RelayCommand(model.)


        //abstract class MyCommand : ICommand
        //{
        //    protected MainWindowViewModel _mainWindowVeiwModel;

        //    public MyCommand(MainWindowViewModel mainWindowVeiwModel)
        //    {
        //        _mainWindowVeiwModel = mainWindowVeiwModel;
        //    }

        //    public event EventHandler CanExecuteChanged;

        //    public abstract bool CanExecute(object parameter);

        //    public abstract void Execute(object parameter);
        //}

        //class OpenChildWindowCommand : MyCommand
        //{
        //    public OpenChildWindowCommand(MainWindowViewModel mainWindowVeiwModel) : base(mainWindowVeiwModel)
        //    {
        //    }
        //    public override bool CanExecute(object parameter)
        //    {
        //        return true;
        //    }
        //    public override void Execute(object parameter)
        //    {
        //        var displayRootRegistry = (Application.Current as App).displayRegistry;

        //        var otherWindowViewModel = new OtherWindowViewModel();
        //        displayRootRegistry.ShowPresentation(otherWindowViewModel);
        //    }
        //}

        //class OpenDialogWindowCommand : MyCommand
        //{
        //    public OpenDialogWindowCommand(MainWindowViewModel mainWindowVeiwModel) : base(mainWindowVeiwModel)
        //    {
        //    }
        //    public override bool CanExecute(object parameter)
        //    {
        //        return true;
        //    }
        //    public override async void Execute(object parameter)
        //    {
        //        var displayRootRegistry = (Application.Current as App).displayRegistry;

        //        var dialogWindowViewModel = new DialogWindowViewModel();
        //        await displayRootRegistry.ShowModalPresentation(dialogWindowViewModel);

        //    }
        //}








    }

}
