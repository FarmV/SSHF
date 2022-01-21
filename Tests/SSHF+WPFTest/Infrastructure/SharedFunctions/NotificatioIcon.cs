using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class NotificatioIcon
    {
        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        public bool NotificationMenuIsOpen = default;
        private Menu_icon? icon;


        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONIDENTIFIER
        {
            public uint SizeStructure; // размер структуры
            public IntPtr handle; //handle родительского окна используемое функцией вызова
            public uint uID;// Определенный приложением идентификатор значка уведомления
            public Guid guid;
        }

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
           

            _notifyIcon.MouseDown += _notifyIcon_MouseDown;
        }

        //public static Rectangle

        private void _notifyIcon_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {

            System.Windows.Forms.MouseButtons buttonMouse = e.Button;

            Rectangle c = NotifyIconHelper.GetIconRect(_notifyIcon);

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

      private class NotifyIconHelper
      {

            public static Rectangle GetIconRect(NotifyIcon icon)
            {
                RECT rect = new RECT();
                NOTIFYICONIDENTIFIER notifyIcon = new NOTIFYICONIDENTIFIER();

                notifyIcon.cbSize = Marshal.SizeOf(notifyIcon);
                //use hWnd and id of NotifyIcon instead of guid is needed
                notifyIcon.hWnd = GetHandle(icon);
                notifyIcon.uID = GetId(icon);

                int hresult = Shell_NotifyIconGetRect(ref notifyIcon, out rect);
                //rect now has the position and size of icon

                return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public Int32 left;
                public Int32 top;
                public Int32 right;
                public Int32 bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NOTIFYICONIDENTIFIER
            {
                public Int32 cbSize; // размер структуры
                public IntPtr hWnd; //handle родительского окна используемое функцией вызова
                public UInt32 uID; //Определенный приложением идентификатор значка уведомления
                public Guid guidItem;
            }

            [DllImport("shell32.dll", SetLastError = true)]
            private static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

            private static FieldInfo? windowField = typeof(NotifyIcon).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            private static IntPtr GetHandle(NotifyIcon icon)
            {
                if (windowField is null) throw new NullReferenceException("Ошибка поиска дескриптора окна иконки");
                NativeWindow? window = windowField.GetValue(icon) as NativeWindow;

                if (window is null) throw new NullReferenceException("Ошибка поиска дескриптора окна иконки"); 
                return window.Handle;
            }

            private static FieldInfo? idField = typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            private static uint GetId(NotifyIcon icon)
            {
              
                if (idField is null) throw new NullReferenceException("Не удалось найти закрытое поле идификатора NotifyIcon");

                return (uint)idField.GetValue(icon);
            }

      }





    }


  

}

