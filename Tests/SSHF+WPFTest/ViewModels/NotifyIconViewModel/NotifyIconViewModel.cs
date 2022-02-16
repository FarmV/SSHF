using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;
using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotifyIconViewModel : ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        

        readonly NotifyIconModel _model;

        public NotifyIconViewModel()
        {
            if (_model is null) _model = new NotifyIconModel(this);
     
        }

        internal void SetCommands(IEnumerable<DataModelCommands> commands)
        {
            foreach (var command in commands)
            {
                CommandsCollecition.Add(command);
            }
        }

        public bool? IsVisible
        {
            get 
            {
                if (App.RegistartorWindows.GetWindow(this) is not Window window1) return null;
                return window1.IsVisible;
            }         
        }

        public ICommand ShutdownAppCommand => new RelayCommand(_model.ShutdownAppExecute, _model.IsExecuteShutdownApp);


        internal ObservableCollection<DataModelCommands> DataCommandsCollection = new ObservableCollection<DataModelCommands>();

        public ObservableCollection<DataModelCommands> CommandsCollecition 
        {
            get => DataCommandsCollection;
        }

        public class DataModelCommands
        {
            public string Content { get; }

            public ICommand Command { get; }

            public DataModelCommands(string content, ICommand command)
            {
                Content = content;
                Command = command;
            }
        }



    }
}
