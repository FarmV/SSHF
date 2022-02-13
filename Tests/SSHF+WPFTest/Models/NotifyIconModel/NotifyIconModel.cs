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
using System.Windows.Interop;
using System.Windows.Media;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure;
using SSHF.Infrastructure.SharedFunctions;

using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.NotifyIcon;


namespace SSHF.Models.NotifyIconModel
{

    internal class NotifyIconModel
    {
        internal static readonly NotifyIcon _notifyIcon = new NotifyIcon();
        internal static Rectangle GetRectanglePosition => NotifyIconHelper.GetIconRect(_notifyIcon);

        internal static volatile bool NotificationMenuIsOpen = default;


        readonly NotifyIconViewModel _iconViewModel;

        public NotifyIconModel(NotifyIconViewModel ViewModel)
        {
            using (_iconViewModel = ViewModel)
                // _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon($"{AppContext.BaseDirectory}{Process.GetCurrentProcess().ProcessName}.exe");
            using( _notifyIcon.Icon = Icon.ExtractAssociatedIcon(@"D:\Downloads\UnderRail GOG\setup_underrail_1.1.4.5_(49811).exe"))
            _notifyIcon.Visible = true;
            _notifyIcon.MouseDown += NotifyIcon_MouseDown;

        }

        private void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        {

            MouseButtons buttonMouse = e.Button;

            if (NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
            {
                App._displayRegistry.HideView(_iconViewModel);

                //App._menu_icon.Hide();
                NotificationMenuIsOpen = false;


                App.Input -= _WindowInput_Input;

                return;
            }
            if (!NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
            {
                App._displayRegistry.ShowView(_iconViewModel);
                App._displayRegistry.HideView(_iconViewModel);
              
                System.Windows.Point pointMenu = GetRectCorrect(App._displayRegistry.GetWindow(_iconViewModel));

                WindowInteropHelper helper = new WindowInteropHelper(App._displayRegistry.GetWindow(_iconViewModel));


                WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(pointMenu.X), Convert.ToInt32(pointMenu.Y), Convert.ToInt32(App._displayRegistry.GetWindow(_iconViewModel).Width), Convert.ToInt32(App._displayRegistry.GetWindow(_iconViewModel).Height), 0x0400);


                App._displayRegistry.ShowView(_iconViewModel);
                
                App.Input += _WindowInput_Input;

                NotificationMenuIsOpen = true;

            }
        }

        private void _WindowInput_Input(object? sender, RawInputEventArgs e)
        {
            RawInputData? data = e.Data;
            RawInputMouseData? mouseData = data as RawInputMouseData;

            if (mouseData is null || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

           
            if (!App._displayRegistry.GetWindow(_iconViewModel).IsVisible) return;
            if (App._displayRegistry.GetWindow(_iconViewModel).IsMouseOver) return;
            if (App._displayRegistry.GetWindow(_iconViewModel).IsVisible)
            {
                Rectangle iconPos = GetRectanglePosition;
                
                System.Windows.Point cursorPos = CursorFunction.GetCursorPosition();


                if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
                {
                    if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
                    if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
                    {
                        App._displayRegistry.HideView(_iconViewModel);
                        
                        NotificationMenuIsOpen = false;

                        
                        App.Input -= _WindowInput_Input;
                        return;

                    }
                    return;
                };
                if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
                {
                    App._displayRegistry.HideView(_iconViewModel);
                   
                    NotificationMenuIsOpen = false;

                   
                    App.Input -= _WindowInput_Input;
                    return;
                }
            }

        }

       

        internal static System.Windows.Point GetRectCorrect(Window window)
        {
            System.Windows.Point point = new System.Windows.Point();

            Rectangle rectIcon = GetRectanglePosition;

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





        public void ShutdownAppExecute(object? parameter) {_notifyIcon.Dispose(); App.Current.Shutdown();}


        public bool IsExecuteShutdownApp(object? parameter) => true;


        //public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject /// невозможно перебрать элементы если окно не отображется
        //{
        //    if (depObj != null)
        //    {
        //        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        //        {
        //            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
        //            if (child != null && child is T)
        //            {
        //                yield return (T)child;
        //            }

        //            foreach (T childOfChild in FindVisualChildren<T>(child))
        //            {
        //                yield return childOfChild;
        //            }
        //        }
        //    }
        //}

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

