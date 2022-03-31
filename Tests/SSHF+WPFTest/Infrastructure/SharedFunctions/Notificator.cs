using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure.Algorithms.Input;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;

using static SSHF.ViewModels.NotifyIconViewModel.NotificatorViewModel;

namespace SSHF.Infrastructure.SharedFunctions
{
    //internal static class Notificator
    //{
    //    static readonly Views.Windows.NotifyIcon.Notificator? Menu_Icon;
    //    static readonly NotificatorViewModel? NotiView;

    //    internal static volatile bool NotificationMenuIsOpen = default;

    //    static Notificator()
    //    {
    //        if (App.WindowsIsOpen.First(sourece => sourece.Tag.ToString() is App.GetWindowNotification) is not Views.Windows.NotifyIcon.Notificator menu_icon) throw new NullReferenceException("Не удалось найти окно нотификации");

    //        if (menu_icon.DataContext is not NotificatorViewModel WidnowNoti) throw new InvalidOperationException("View model не соответсвуе тожидаймой");

    //        Menu_Icon = menu_icon;
    //        NotiView = WidnowNoti;
    //    }

    //    internal static void SetCommand(IEnumerable<DataModelCommands>? commands)
    //    {
    //        if(NotiView is null) throw new ArgumentNullException(nameof(NotiView), "SetCommand NotiView is NULL");

           
    //        if (commands is not null) NotiView.SetCommands(commands);


    //        if (Menu_Icon is null) throw new ArgumentNullException(nameof(NotiView), "SetCommand Menu_Icon is NULL");

    //       // System.Windows.Point pointMenu = SSHF.Models.NotifyIconModel.NotificatorModel.GetRectCorrect(Menu_Icon);

    //        WindowInteropHelper helper = new WindowInteropHelper(Menu_Icon);

    //        System.Windows.Point PointCursor = CursorFunction.GetCursorPosition();
            
    //        NotiShow(Menu_Icon);
    //    }

    //    static void NotiShow(Window? window)
    //    {
    //        if (window is null) return;

    //        WindowInteropHelper helper = new WindowInteropHelper(window);

    //        System.Windows.Point PointCursor = CursorFunction.GetCursorPosition();



    //        window?.Show();
    //        WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(PointCursor.X), Convert.ToInt32(PointCursor.Y), Convert.ToInt32(window.Width), Convert.ToInt32(window.Height), 0x0400);
            
    //        NotificationMenuIsOpen = true;

    //        App.Input += Raw_InputMouse;


    //    }

    //    private static void Raw_InputMouse(object? sender, RawInputEvent e)
    //    {
    //        RawInputData? data = e.Data;

    //        if (data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

    //        if (Menu_Icon is null) throw new NullReferenceException("Menu_Icon is NULL");

    //        if (Menu_Icon.IsVisible is false) return;
    //        if (Menu_Icon.IsMouseOver) return;

    //        Menu_Icon?.Hide();
    //        NotificationMenuIsOpen = false;

    //        if (NotiView is null) throw new NullReferenceException("NotiView is NULL");

    //        NotiView.DataCommandsCollection.Clear();

    //        App.Input -= Raw_InputMouse;
    //    }
    //}
}
