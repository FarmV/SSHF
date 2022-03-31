using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure;
using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;
using SSHF.Views.Windows.Notify;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotificatorViewModel: ViewModel
    {
        private readonly Window GeneralNotificatorWindow;
        private readonly NotificatorModel _model;
        public bool NotificatorIsOpen => GeneralNotificatorWindow.IsVisible;
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
        public NotificatorViewModel(Window generalNotificator)
        {
            _model = _model is not null ? _model : new NotificatorModel(this);
            GeneralNotificatorWindow = generalNotificator;
        }

        private ObservableCollection<DataModelCommands> DataCommandsCollection = new ObservableCollection<DataModelCommands>();

        public ObservableCollection<DataModelCommands> CommandsCollecition => DataCommandsCollection;

        public class DataModelCommands
        {
            public string Content
            {
                get;
            }
            public ICommand Command
            {
                get;
            }
            public DataModelCommands(string content, ICommand command)
            {
                Content = content; Command = command;
            }
        }
        private void AddCommandsToIvoceLIst(IEnumerable<DataModelCommands> commands) => Array.ForEach(commands.ToArray(), (x) => { CommandsCollecition.Add(x); });


        private EventWaitHandle WaitHandleClearCollection = new EventWaitHandle(false, EventResetMode.AutoReset);
        public void SetPositionInvoke(IEnumerable<DataModelCommands> commands, System.Windows.Point showPostionWindow, Rectangle ignoreAreaClick = default)
        {
            AddCommandsToIvoceLIst(commands);


            System.Windows.Point cursorPos = showPostionWindow == default ? CursorFunction.GetCursorPosition() : showPostionWindow;
            Rectangle iconPos = ignoreAreaClick == default ? default : ignoreAreaClick;


            WindowInteropHelper helper = new WindowInteropHelper(GeneralNotificatorWindow);
            
            WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(showPostionWindow.X), Convert.ToInt32(showPostionWindow.Y),
                Convert.ToInt32(GeneralNotificatorWindow.Width), Convert.ToInt32(GeneralNotificatorWindow.Height), 0x0400| 0x0040);

            _ = Task.Run(() =>
            {
                WaitHandleClearCollection.WaitOne();
                GeneralNotificatorWindow.Dispatcher.Invoke(() =>
                {
                    GeneralNotificatorWindow.Hide();
                    DataCommandsCollection.Clear();
                    CommandsCollecition.Clear();
                });
            });


            void App_Input(object? sender, RawInputEvent e)
            {
                if (e.Data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

                if (GeneralNotificatorWindow.IsVisible is false) return;
                if (GeneralNotificatorWindow.IsMouseOver) return;

                if (ignoreAreaClick == default)
                {
                    WaitHandleClearCollection.Set();
                    App.Input -= App_Input;
                }
                else
                {
                    System.Windows.Point cursorЗosition = CursorFunction.GetCursorPosition();
                }



            }

            App.Input += App_Input;
        }

    }

}
