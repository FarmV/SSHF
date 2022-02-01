using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

using SSHF.Infrastructure.SharedFunctions;

using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.NotifyIcon;


namespace SSHF.Models.NotifyIconModel
{

    internal class NotifyIconModel
    {
        public static readonly NotifyIcon _notifyIcon = new NotifyIcon();
        public static Rectangle GetRectanglePosition => NotifyIconHelper.GetIconRect(_notifyIcon);

        public static volatile bool NotificationMenuIsOpen = default;


        readonly NotifyIconViewModel _iconViewModel;

        readonly static Action DropHandelrHook = new Action(() =>
        {
            App.mouseHook.LeftButtonUp -= MouseHookHandler;
            App.mouseHook.DoubleClick -= MouseHookHandler;
            App.mouseHook.RightButtonUp -= MouseHookHandler;
            App.mouseHook.MouseWheel -= MouseHookHandler;
        });

        public NotifyIconModel(NotifyIconViewModel ViewModel)
        {
            _iconViewModel = ViewModel;
            // _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon($"{AppContext.BaseDirectory}{Process.GetCurrentProcess().ProcessName}.exe");
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(@"D:\Downloads\UnderRail GOG\setup_underrail_1.1.4.5_(49811).exe");
            _notifyIcon.Visible = true;
            _notifyIcon.MouseDown += NotifyIcon_MouseDown;
            App.mouseHook.Install();
        }

        private void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {

            MouseButtons buttonMouse = e.Button;

            if (NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Left)
            {
                App._menu_icon.Hide();
                NotificationMenuIsOpen = false;

                DropHandelrHook.Invoke();


                return;
            }
            if (!NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Left)
            {
                var pointMenu = GetRectCorrect(App._menu_icon as Window);
                App._menu_icon.Left = pointMenu.X;
                App._menu_icon.Top = pointMenu.Y;
                App._menu_icon.Topmost = true;
                App._menu_icon.Show();


                App.mouseHook.LeftButtonUp += MouseHookHandler;
                App.mouseHook.DoubleClick += MouseHookHandler;
                App.mouseHook.RightButtonUp += MouseHookHandler;
                App.mouseHook.MouseWheel += MouseHookHandler;

                NotificationMenuIsOpen = true;

            }

        }

        private static void MouseHookHandler(GlobalLowLevelHooks.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            if (!App._menu_icon.IsVisible) return;
            if (App._menu_icon.IsMouseOver) return;
            if (App._menu_icon.IsVisible)
            {
                Rectangle iconPos = GetRectanglePosition;
                System.Windows.Point cursorPos = CursorFunction.GetCursorXY(App._menu_icon);


                if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
                {
                    if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
                    if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
                    {
                        App._menu_icon.Hide();
                        NotificationMenuIsOpen = false;

                        DropHandelrHook.Invoke();
                        return;

                    }
                    return;
                };
                if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
                {
                    App._menu_icon.Hide();
                    NotificationMenuIsOpen = false;

                    DropHandelrHook.Invoke();
                    return;
                }
            }
        }

        private System.Windows.Point GetRectCorrect(Window window)
        {
            System.Windows.Point point = new System.Windows.Point();

            Rectangle rectIcon = GetRectanglePosition;

            window.Hide();
            window.Show();

            point.Y = rectIcon.Y - window.Height + rectIcon.Size.Height / 2;
            point.X = rectIcon.X - window.Width;



            return point;
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


        #region Проверка клика вне окна

        public void CheckClickOutsideExecute(object? parameter)
        {

            App._menu_icon.Hide();
        }
        public bool IsExecuteCheckClickOutside(object? parameter)
        {


            return true;
        }




        #endregion


    }


}

