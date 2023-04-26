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

using FVH.Background.Input;

using Linearstar.Windows.RawInput;
using SSHF.Infrastructure;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.Models.NotifyIconModel;
using SSHF.Views.Windows.Notify;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotificatorViewModel
    {
       // private Window GeneralNotificatorWindow => App.WindowsIsOpen[App.GetWindowNotification].Key;
        private Window _trayIconWindow;
        private IMouseHandler _mouseHandler;
        private TrayIconManager _trayIconManager;
        public NotificatorViewModel(Window windowNotificator, TrayIconManager trayIcon, IMouseHandler mouseHandler)
        {
            _trayIconManager = trayIcon;
            _trayIconWindow = windowNotificator;
            _mouseHandler = mouseHandler;
        }     
        public bool NotificatorIsOpen => _trayIconWindow.IsVisible;

      
        //public async Task SetPositionInvoke(IEnumerable<DataModelCommands> commands, System.Windows.Point showPostionWindow, Rectangle ignoreAreaClick = default)
        //{
        //    if (NotificatorIsOpen is true)
        //    {
        //        _trayIconWindow.Dispatcher.Invoke(() =>
        //        {
        //            _trayIconWindow.Hide();
        //            DataCommandsCollection.Clear();
        //            CommandsCollecition.Clear();
        //        });
        //    }

        //    await _trayIconWindow.Dispatcher.BeginInvoke(() =>
        //    {
        //        App.WindowsIsOpen[App.GetWindowNotification].Value.Invoke((IEnumerable<DataModelCommands> comm) =>
        //        {
        //            Array.ForEach(comm.ToArray(), command => CommandsCollecition.Add(command));
        //        }, DispatcherPriority.Render, commands);
        //    });

        //    _trayIconWindow.Show();

        //    System.Windows.Point cursorShowPosition = showPostionWindow == default ? CursorFunctions.GetCursorPosition() : showPostionWindow;
        //    Rectangle iconPos = ignoreAreaClick == default ? default : ignoreAreaClick;


        //    WindowInteropHelper helper = new WindowInteropHelper(_trayIconWindow);

        //    bool res = Win32ApiWindow.RefreshWindowPositin.SetWindowPos(helper.Handle, -1, Convert.ToInt32(cursorShowPosition.X), Convert.ToInt32(cursorShowPosition.Y),
        //    Convert.ToInt32(_trayIconWindow.Width), Convert.ToInt32(_trayIconWindow.Height), 0x0400 | 0x0040);

        //    _ = Task.Run(() =>
        //    {
        //        WaitHandleClearCollection.WaitOne();
        //        _trayIconWindow.Dispatcher.Invoke(() =>
        //        {
        //            _trayIconWindow.Hide();
        //            DataCommandsCollection.Clear();
        //            CommandsCollecition.Clear();
        //        });
        //    });


        //    void AppInputMouse(object? sender, RawInputEvent e)
        //    {
        //        if (e.Data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

        //        if (_trayIconWindow.IsVisible is false) return;
        //        if (_trayIconWindow.IsMouseOver) return;

        //        if (ignoreAreaClick == default)
        //        {
        //            WaitHandleClearCollection.Set();
        //            App.Input -= AppInputMouse;
        //        }
        //        else
        //        {
        //            System.Windows.Point cursorPos = CursorFunctions.GetCursorPosition();
        //            if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
        //            {
        //                if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
        //                if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
        //                {
        //                    WaitHandleClearCollection.Set();
        //                    App.Input -= AppInputMouse;
        //                    return;
        //                }
        //                return;
        //            };
        //            if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
        //            {
        //                WaitHandleClearCollection.Set();
        //                App.Input -= AppInputMouse;
        //                return;
        //            }
        //        }
        //    }

        //    App.Input += AppInputMouse;
        //}
        //internal Task CloseNotificatorAsync() => Task.Run(() =>
        //{
        //    _ = _trayIconWindow.InvokeAsync(() =>
        //    {
        //        _trayIconWindow.Dispatcher.Invoke(() =>
        //        {
        //            _trayIconWindow.Hide();
        //            DataCommandsCollection.Clear();
        //            CommandsCollecition.Clear();
        //        });
        //    });
        //});
        //internal async void CloseNotificator()
        //{
        //    await _trayIconWindow.InvokeAsync(() =>
        //    {
        //        _trayIconWindow.Dispatcher.Invoke(() =>
        //        {
        //            _trayIconWindow.Hide();
        //            DataCommandsCollection.Clear();
        //            CommandsCollecition.Clear();
        //        });
        //    });
        //}
    }

}
