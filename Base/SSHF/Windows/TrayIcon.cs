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
using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;

using static SSHF.ViewModels.NotifyIconViewModel.NotificatorViewModel;
using static SSHF.ViewModels.TrayIconManager;

namespace SSHF.ViewModels
{
    internal class TrayIconManager : IAsyncDisposable
    {
        private readonly Window _notificatorWindow;

        public TrayIconManager(Window notificatorWindow)
        {
            _notificatorWindow = notificatorWindow;
            App.DpiChange += (_, _) =>
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
                    Icon = Icon.ExtractAssociatedIcon(@"C:\Windows\winhlp32.exe"),
                    Visible = true
                };
                _notifyIcon.MouseDown += NotifyIcon_MouseDown;
            }
            _notifyIcon.MouseDown += NotifyIcon_MouseDown;
        }
        public async ValueTask DisposeAsync() => await Task.Run(() => _notifyIcon.Dispose()); 
        private NotifyIcon _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(@"C:\Windows\winhlp32.exe"),
            Visible = true
        };
        //private readonly NotificatorViewModel Notificator = App.GetNotificator();


        private  Rectangle GetRectanglePosition()
        {
            return NotifyIconHelper.GetIconRectangle(_notifyIcon);
        }

        public enum RectPos
        {
            Centre = 0,
            Left = 1,
            Top = 2,
            Right = 3,
            Bottom = 4,
        }
        private System.Windows.Point GetOffsetPointInRectangle(Rectangle rectangleIcon, RectPos posOutput)
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

        bool blockOne = false;
        private void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {
           if (blockOne is true) return;
           blockOne = true;
           if (System.Windows.MessageBox.Show("Закрыть приложение?","Запрос SSHF",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No) is MessageBoxResult.Yes)
           {
                
                this._notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
                return;
           }
           blockOne = false;
            //if (_notificatorWindow.Visibility is Visibility.Visible)
            //{
            //   // VisibilityChange?.Invoke(this, Visibility.Hidden);
            //    _notificatorWindow.Visibility = Visibility.Hidden;
            //    return;
            //}

            //if (_notificatorWindow.Visibility is Visibility.Hidden && e.Button is MouseButtons.Right || e.Button is MouseButtons.Left)
            //{
            //  //  VisibilityChange?.Invoke(this, Visibility.Visible);
            //    System.Windows.Point point = GetOffsetPointInRectangle(NotifyIconHelper.GetIconRectangle(_notifyIcon), RectPos.Bottom);
            //    _notificatorWindow.Visibility = Visibility.Visible;

            //    _notificatorWindow.Left = point.X;
            //    _notificatorWindow.Top = point.Y;

            //    return;
            //}

            //if (Notificator.NotificatorIsOpen is not true && e.Button is MouseButtons.Right)
            //{                           
            //    await Notificator.SetPositionInvoke(new DataModelCommands[1] { new DataModelCommands("Закрыть",
            //       new RelayCommand((obj) => 
            //       {
            //           App.WindowsIsOpen[App.GetMyMainWindow].Value.BeginInvokeShutdown(DispatcherPriority.Normal);
            //           App.WindowsIsOpen[App.GetWindowNotification].Value.BeginInvokeShutdown(DispatcherPriority.Normal);
            //           App.Current.Dispatcher.Invoke(() => {App.Current.Shutdown();});
            //       }))}, GetPositinRectangle(NotifyIconHelper.GetIconRect(_notifyIcon), RectPos.Bottom),
            //       NotifyIconHelper.GetIconRect(_notifyIcon));
            //    return;
            //}
        }

        internal System.Windows.Point GetRectCorrect(Window window)
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


        internal System.Windows.Size GetElementPixelSize(UIElement element)
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
            public static Rectangle GetIconRectangle(NotifyIcon icon)
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
            private static IntPtr GetHandle(NotifyIcon icon)=>  windowField is null ? throw new NullReferenceException("Ошибка поиска дескриптора окна иконки") :
                    windowField.GetValue(icon) is not NativeWindow window ? throw new NullReferenceException("Ошибка поиска дескриптора окна иконки") : window.Handle;
            

            private static FieldInfo? idField = typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            private static uint GetId(NotifyIcon icon)
            {

                return idField is not null ? Convert.ToUInt32(idField.GetValue(icon)): throw new NullReferenceException("Не удалось найти закрытое поле идификатора NotifyIcon");
            }

        }


    }
}
