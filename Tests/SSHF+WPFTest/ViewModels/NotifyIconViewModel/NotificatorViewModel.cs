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
using System.Windows.Threading;

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
        private Window GeneralNotificatorWindow => App.WindowsIsOpen[App.GetWindowNotification].Key;
        private readonly NotificatorModel _model;
        public bool NotificatorIsOpen => GeneralNotificatorWindow.IsVisible;
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
        public NotificatorViewModel()
        {
            _model = _model is not null ? _model : new NotificatorModel(this);
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


        private readonly EventWaitHandle WaitHandleClearCollection = new EventWaitHandle(false, EventResetMode.AutoReset);
        public async Task SetPositionInvoke(IEnumerable<DataModelCommands> commands, System.Windows.Point showPostionWindow, Rectangle ignoreAreaClick = default)
        {
            if (NotificatorIsOpen is true)
            {
                GeneralNotificatorWindow.Dispatcher.Invoke(() =>
                {
                    GeneralNotificatorWindow.Hide();
                    DataCommandsCollection.Clear();
                    CommandsCollecition.Clear();
                });
            }

            await GeneralNotificatorWindow.Dispatcher.BeginInvoke(() =>
            {
                App.WindowsIsOpen[App.GetWindowNotification].Value.Invoke((IEnumerable<DataModelCommands> comm) =>
                {
                    Array.ForEach(comm.ToArray(), command => CommandsCollecition.Add(command));
                }, DispatcherPriority.Render, commands);
            });

            GeneralNotificatorWindow.Show();

            System.Windows.Point cursorShowPosition = showPostionWindow == default ? CursorFunction.GetCursorPosition() : showPostionWindow;
            Rectangle iconPos = ignoreAreaClick == default ? default : ignoreAreaClick;


            WindowInteropHelper helper = new WindowInteropHelper(GeneralNotificatorWindow);

            bool res = WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(cursorShowPosition.X), Convert.ToInt32(cursorShowPosition.Y),
            Convert.ToInt32(GeneralNotificatorWindow.Width), Convert.ToInt32(GeneralNotificatorWindow.Height), 0x0400 | 0x0040);

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


            void AppInputMouse(object? sender, RawInputEvent e)
            {
                if (e.Data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

                if (GeneralNotificatorWindow.IsVisible is false) return;
                if (GeneralNotificatorWindow.IsMouseOver) return;

                if (ignoreAreaClick == default)
                {
                    WaitHandleClearCollection.Set();
                    App.Input -= AppInputMouse;
                }
                else
                {
                    System.Windows.Point cursorPos = CursorFunction.GetCursorPosition();
                    if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
                    {
                        if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
                        if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
                        {
                            WaitHandleClearCollection.Set();
                            App.Input -= AppInputMouse;
                            return;
                        }
                        return;
                    };
                    if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
                    {
                        WaitHandleClearCollection.Set();
                        App.Input -= AppInputMouse;
                        return;
                    }
                }
            }

            App.Input += AppInputMouse;
        }
        internal Task CloseNotificatorAsync() => Task.Run(() =>
        {
            _ = App.WindowsIsOpen[App.GetWindowNotification].Value.InvokeAsync(() =>
            {
                GeneralNotificatorWindow.Dispatcher.Invoke(() =>
                {
                    GeneralNotificatorWindow.Hide();
                    DataCommandsCollection.Clear();
                    CommandsCollecition.Clear();
                });
            });
        });
        internal async void CloseNotificator()
        {
            await App.WindowsIsOpen[App.GetWindowNotification].Value.InvokeAsync(() =>
            {
                GeneralNotificatorWindow.Dispatcher.Invoke(() =>
                {
                    GeneralNotificatorWindow.Hide();
                    DataCommandsCollection.Clear();
                    CommandsCollecition.Clear();
                });
            });
        }
    }

}
