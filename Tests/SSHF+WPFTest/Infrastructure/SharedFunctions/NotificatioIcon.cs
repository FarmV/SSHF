using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class NotificatioIcon
    {
        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        public bool NotificationMenuIsOpen = default;

        public NotificatioIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            SetIconToMainApplication();
        }

        private void SetIconToMainApplication()
        {
            // _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon($"{AppContext.BaseDirectory}{Process.GetCurrentProcess().ProcessName}.exe");
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(@"D:\Downloads\UnderRail GOG\setup_underrail_1.1.4.5_(49811).exe");
            _notifyIcon.Visible = true;
            // _notifyIcon.Click += ClickNotifyIcon;

            _notifyIcon.MouseDown += _notifyIcon_MouseDown;
        }

        private void _notifyIcon_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {

            System.Windows.Forms.MouseButtons buttonMouse = e.Button;



            if (NotificationMenuIsOpen && buttonMouse == System.Windows.Forms.MouseButtons.Left)
            {
                icon?.Close();
                NotificationMenuIsOpen = false;
                return;
            }
            if (!NotificationMenuIsOpen && buttonMouse == System.Windows.Forms.MouseButtons.Left)
            {
                icon = new Menu_icon();
                NotificationMenuIsOpen = true;

            }
        }

        Menu_icon? icon;
        private void ClickNotifyIcon(object? sender, EventArgs e)
        {

            if (NotificationMenuIsOpen)
            {

                icon?.Close();
                NotificationMenuIsOpen = false;
                return;
            }
            icon = new Menu_icon();
            NotificationMenuIsOpen = true;
            icon.Show();
        }


        //public void ClickNotifyIcon(object? sender, EventArgs e)
        //{
        //    if (NotificationMenuIsOpen)
        //    {
        //        WindowCollection adssad = App.Current.Windows;

        //        foreach (var item in App.Current.Windows)
        //        {
        //            if (item is WPF_Traslate_Test.MenuContent)
        //            {
        //                MenuContent menu = (MenuContent)item;
        //                //menu.Dispose();
        //                MenuCheckedButton.Clear();
        //                foreach (RadioButton radioButton in MainWindow.FindVisualChildren<RadioButton>(menu))
        //                {
        //                    if (radioButton.IsChecked == true)
        //                    {
        //                        MenuCheckedButton.Add(radioButton.Name, true);

        //                    }
        //                }
        //                menu.Close();
        //                NotificationMenuIsOpen = false;
        //            }
        //        }
        //        return;
        //    }
        //NotificationMenuIsOpen = true;
        //App.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        //One.Visibility = Visibility.Collapsed;
        //One.Hide();
        //MenuContent menuContent = new MenuContent(this);
        //Point positionCursor = GetCursorPosition();
        //menuContent.Topmost = true;
        //double resolutionWidth = SystemParameters.PrimaryScreenWidth;
        //double resolutionHeight = SystemParameters.PrimaryScreenHeight;

        //WindowCollection windowsMyApp = App.Current.Windows;

        //double posT = menuContent.Top = positionCursor.Y - 200.00;
        //double posL = menuContent.Left = positionCursor.X + 5;
        //menuContent.Show();
        //double menuWidth = default;
        //double menuHeight = default;
        //foreach (var item in App.Current.Windows)
        //{
        //    if (item is WPF_Traslate_Test.MenuContent)
        //    {
        //        MenuContent menu = (MenuContent)item;

        //        menuWidth = menu.ActualWidth;
        //        menuHeight = menu.ActualHeight;
        //    }
        //}

        //if (menuWidth + positionCursor.X > resolutionWidth)
        //{
        //    menuContent.Left = resolutionWidth - menuWidth;
        //    menuContent.Left = positionCursor.X - (menuWidth + 5);
        //}
        //if (menuHeight + positionCursor.Y < resolutionHeight)
        //{
        //    menuContent.Top = resolutionHeight - menuHeight;
        //    menuContent.Top = positionCursor.Y - (menuHeight - 200.00);
        //}

        // }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject /// невозможно перебрать элементы если окно не отображется
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
