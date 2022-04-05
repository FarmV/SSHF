using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;

using static SSHF.ViewModels.NotifyIconViewModel.NotificatorViewModel;

namespace SSHF.ViewModels
{
    internal class TrayIcon
    {
        //private static TrayIcon? Instance;
        private static NotifyIcon? _notifyIcon;
        private NotificatorViewModel Notificator = App.GetNotificator();

        public TrayIcon()
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
            }
            Init();
        }


        private static Rectangle GetRectanglePosition()
        {
            return NotifyIconHelper.GetIconRect(_notifyIcon);
        }

        private enum RectPos
        {
            Centre = 0,
            Left = 1,
            Top = 2,
            Right = 3,
            Bottom = 4,
        }
        private System.Windows.Point GetPositinRectangle(Rectangle rectangleIcon, RectPos posOutput)
        {
            int xCentre = rectangleIcon.Location.X + rectangleIcon.Size.Width / 2;
            int yCentre = rectangleIcon.Location.Y + rectangleIcon.Size.Height / 2;
            if (posOutput is RectPos.Centre) return new System.Windows.Point(xCentre, yCentre);
            if (posOutput is RectPos.Left) return new System.Windows.Point(xCentre - rectangleIcon.Size.Width / 2, yCentre);
            if (posOutput is RectPos.Right) return new System.Windows.Point(xCentre + rectangleIcon.Size.Width / 2, yCentre);
            if (posOutput is RectPos.Top) return new System.Windows.Point(rectangleIcon.Location.X + rectangleIcon.Size.Width / 2, rectangleIcon.Location.Y);
            if (posOutput is RectPos.Bottom) return new System.Windows.Point(rectangleIcon.Location.X + rectangleIcon.Size.Width / 2, rectangleIcon.Location.Y + rectangleIcon.Size.Height);
            throw new InvalidOperationException();
        }

        private async void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {
            if (Notificator.NotificatorIsOpen && e.Button is MouseButtons.Right)
            {
                Notificator.CloseNotificator(); return;
            }

            if (!Notificator.NotificatorIsOpen && e.Button is MouseButtons.Right)
            {                           
                await Notificator.SetPositionInvoke(new DataModelCommands[1] { new DataModelCommands("Закрыть",
                   new RelayCommand((obj) => 
                   {
                       App.WindowsIsOpen[App.GetMyMainWindow].Value.BeginInvokeShutdown(DispatcherPriority.Normal);
                       App.WindowsIsOpen[App.GetWindowNotification].Value.BeginInvokeShutdown(DispatcherPriority.Normal);
                       App.Current.Dispatcher.Invoke(() => {App.Current.Shutdown();});
                   }))}, GetPositinRectangle(NotifyIconHelper.GetIconRect(_notifyIcon), RectPos.Bottom),
                   NotifyIconHelper.GetIconRect(_notifyIcon));
                return;
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
