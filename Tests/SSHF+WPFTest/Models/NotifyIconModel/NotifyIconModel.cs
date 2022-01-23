using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SSHF.Infrastructure.SharedFunctions;
using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.Models.NotifyIconModel
{
    internal class NotifyIconModel
    {
       //private readonly Menu_icon icon;

       public NotifyIconModel()
       {
            //icon = new Menu_icon();
            NotificatioIcon._notifyIcon.MouseDown += _notifyIcon_MouseDown;
       }

        private void _notifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {
            MessageBox.Show("Test255");
           
        }

        //private void NotifyIcon_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        //{

        //    System.Windows.Forms.MouseButtons buttonMouse = e.Button;

        //    if (NotificatioIcon.NotificationMenuIsOpen && buttonMouse == System.Windows.Forms.MouseButtons.Left)
        //    {
        //        icon.Hide();
        //        NotificatioIcon.NotificationMenuIsOpen = false;
        //        return;
        //    }
        //    if (!NotificatioIcon.NotificationMenuIsOpen && buttonMouse == System.Windows.Forms.MouseButtons.Left)
        //    {
        //        icon.Show();
        //        NotificatioIcon.NotificationMenuIsOpen = true;

        //    }
        //}

    }


}

