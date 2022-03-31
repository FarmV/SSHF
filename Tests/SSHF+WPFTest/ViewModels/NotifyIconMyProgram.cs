using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;

namespace SSHF.ViewModels
{
    internal class NotifyIconMyProgram
    {
        private static NotifyIconMyProgram? Instance;
        private static NotifyIcon? _notifyIcon;

        internal static NotifyIconMyProgram GetInstance() => Instance is not null ? Instance : new NotifyIconMyProgram();

        private NotifyIconMyProgram()
        {
            App.DPIChange += (obj, ev) =>
            {
                try
                {
                    if (_notifyIcon is not null) _notifyIcon.MouseDown -= NotifyIcon_MouseDown;
                    if (_notifyIcon is not null) _notifyIcon?.Dispose();
                }
                finally { Init(); }
            };

            void Init()
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files\nodejs\node.exe"),
                    Visible = true
                };
                _notifyIcon.MouseDown += NotifyIcon_MouseDown;
            }Init();
        }


        private static Rectangle GetRectanglePosition()
        {
            if (_notifyIcon is null) return new Rectangle();
            return NotifyIconHelper.GetIconRect(_notifyIcon);
        }

        private void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {

            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (App.WindowsIsOpen.First(result => result.Tag.ToString() is App.GetWindowNotification) is not Menu_icon NotificationMenu) throw new NullReferenceException("Окно нотификации не найдено");

                MouseButtons buttonMouse = e.Button;

                if (Notificator.NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
                {

                    NotificationMenu.Hide();
                    _iconViewModel.DataCommandsCollection.Clear();
                    //if (menu_Icon.IsVisible) return;
                    //if (App.RegistartorWindows.HideView(_iconViewModel) is false) return;


                    Notificator.NotificationMenuIsOpen = false;


                    App.Input -= _WindowInput_Input;

                    return;
                }
                if (!Notificator.NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
                {
                    // App.RegistartorWindows.ShowView(_iconViewModel);
                    // App.RegistartorWindows.HideView(_iconViewModel);



                    System.Windows.Point pointMenu = GetRectCorrect(NotificationMenu);

                    WindowInteropHelper helper = new WindowInteropHelper(NotificationMenu);


                    WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(pointMenu.X), Convert.ToInt32(pointMenu.Y), Convert.ToInt32(NotificationMenu.Width), Convert.ToInt32(NotificationMenu.Height), 0x0400);


                    NotificationMenu.Show();

                    App.Input += _WindowInput_Input;

                    Notificator.NotificationMenuIsOpen = true;

                }
            }));
        }

        private void _WindowInput_Input(object? sender, RawInputEvent e)
        {
            if (e.Data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
 
            Window[]? Window = App.WindowsIsOpen.Where(result => result.Tag.ToString() is App.GetWindowNotification).ToArray();

            Notificator? notificatorXamlWindow = Window.Length > 1 || Window.Length == 0 ?
                throw new NullReferenceException("Окно нотификации не найдено") : Window[0] as Notificator ?? throw new Exception($"{typeof(Notificator)}");

            if (notificatorXamlWindow.DataContext is not NotificatorViewModel notificator) throw new NullReferenceException("Не найден ожидаемый контекст данных для окна");


            if (notificatorXamlWindow.IsVisible is false) return;
            if (notificatorXamlWindow.IsMouseOver) return;
            if (notificatorXamlWindow.IsVisible)
            {
                Rectangle iconPos = GetRectanglePosition();

                System.Windows.Point cursorPos = CursorFunction.GetCursorPosition();


                if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
                {
                    if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
                    if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
                    {
                        notificatorXamlWindow.Hide();
                        notificator.NotificatorIsOpen = false;


                        App.Input -= _WindowInput_Input;
                        return;

                    }
                    return;
                };
                if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
                {
                    notificatorXamlWindow.Hide();
                    _iconViewModel.DataCommandsCollection.Clear();

                    notificatorXamlWindow.NotificationMenuIsOpen = false;


                    App.Input -= _WindowInput_Input;
                    return;
                }
            }

        }



        internal static System.Windows.Point GetRectCorrect(Window window)
        {
            System.Windows.Point point = new System.Windows.Point();

            Rectangle rectIcon = GetRectanglePosition();

            System.Windows.Size elementWindow = GetElementPixelSize(window);

            int pixelWidth = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, elementWindow.Width));
            int pixelHeight = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, elementWindow.Height));

            point.Y = rectIcon.Y - pixelHeight + rectIcon.Size.Height / 2;
            point.X = rectIcon.X - pixelWidth;

            return point;
        }


        internal static System.Windows.Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;

            PresentationSource? source = PresentationSource.FromVisual(element);

            //DpiScale aa = VisualTreeHelper.GetDpi(element);

            if (source is not null)
            {
                transformToDevice = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using var source2 = new HwndSource(new HwndSourceParameters());

                transformToDevice = source2.CompositionTarget.TransformToDevice;
            }

            if (element.DesiredSize == new System.Windows.Size())
            {
                element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            return (System.Windows.Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        internal class NotifyIconHelper
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
