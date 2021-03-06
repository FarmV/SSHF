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
using System.Windows.Threading;

using Linearstar.Windows.RawInput;

using SSHF.Infrastructure;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.SharedFunctions;

using SSHF.ViewModels.NotifyIconViewModel;
using SSHF.Views.Windows.Notify;


namespace SSHF.Models.NotifyIconModel
{

    internal class NotificatorModel
    {
        readonly NotificatorViewModel IconViewModel;
        public NotificatorModel(NotificatorViewModel iconViewModel)
        {
            IconViewModel = iconViewModel;
        }

       // readonly NotificatorViewModel _iconViewModel;
        // internal static NotifyIcon? _notifyIcon;
        // internal static Rectangle GetRectanglePosition()
        // {
        //     if (_notifyIcon is null) return new Rectangle();
        //     return NotifyIconHelper.GetIconRect(_notifyIcon);
        // }




        ////internal static volatile bool NotificationMenuIsOpen = default;



        // void InitialIcon()
        // {
        //     _notifyIcon = new NotifyIcon
        //     {
        //         Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files\nodejs\node.exe"),
        //         Visible = true
        //     };
        //     _notifyIcon.MouseDown += NotifyIcon_MouseDown;

        // }

        // public NotificatorModel(NotificatorViewModel ViewModel)
        // {
        //    // DisposeTimer();
        //     using (_iconViewModel = ViewModel)
        //         // _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon($"{AppContext.BaseDirectory}{Process.GetCurrentProcess().ProcessName}.exe");
        //         InitialIcon();

        //     // myTimer();
        //     App.CheckCount++;

        //     App.DPIChange += (obj, ev) =>
        //     {
        //         try
        //         {
        //             if (_notifyIcon is not null)
        //                 _notifyIcon.MouseDown -= NotifyIcon_MouseDown;
        //             if (_notifyIcon is not null)
        //                 _notifyIcon?.Dispose();
        //         }
        //         finally
        //         {
        //             InitialIcon();
        //         }
        //     };
        // }

        ////static void DisposeTimer()
        //// {
        ////     System.Threading.Timer timer = new System.Threading.Timer(new System.Threading.TimerCallback((obj) =>
        ////     {
        ////         try
        ////         {
        ////             App.Current.Dispatcher.Invoke(new Action(() =>
        ////             { 
        ////              if (App.WindowsIsOpen.First(result => result.Tag.ToString() is App.GetWindowNotification) is not Menu_icon NotificationMenu) throw new NullReferenceException("Окно нотификации не найдено");
        ////             }));
        ////         }
        ////         catch (Exception ex)
        ////         {
        ////             System.Diagnostics.Debug.WriteLine(ex);
        ////         }
        ////         finally
        ////         {
        ////             _notifyIcon?.Dispose();

        ////             App.Current.Dispatcher?.Invoke(new Action(() => { App.Current?.Shutdown(); }));
        ////             Environment.Exit(1359);
        ////         }
        ////     }), null, 5000, 5000);
        //// }





        // private void NotifyIcon_MouseDown(object? sender, MouseEventArgs e)
        // {

        //     App.Current.Dispatcher.Invoke(new Action(() =>
        //     {
        //         if (App.WindowsIsOpen.First(result => result.Tag.ToString() is App.GetWindowNotification) is not Views.Windows.NotifyIcon.Notificator NotificationMenu) throw new NullReferenceException("Окно нотификации не найдено");

        //          MouseButtons buttonMouse = e.Button;

        //     if (Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
        //     {

        //         NotificationMenu.Hide();
        //             _iconViewModel.DataCommandsCollection.Clear();
        //             //if (menu_Icon.IsVisible) return;
        //             //if (App.RegistartorWindows.HideView(_iconViewModel) is false) return;


        //             Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen = false;


        //             App.Input -= _WindowInput_Input;

        //         return;
        //     }
        //     if (!Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen && buttonMouse is System.Windows.Forms.MouseButtons.Right)
        //     {
        //             // App.RegistartorWindows.ShowView(_iconViewModel);
        //             // App.RegistartorWindows.HideView(_iconViewModel);



        //             System.Windows.Point pointMenu = GetRectCorrect(NotificationMenu);

        //             WindowInteropHelper helper = new WindowInteropHelper(NotificationMenu);


        //             WindowFunction.SetWindowPos(helper.Handle, -1, Convert.ToInt32(pointMenu.X), Convert.ToInt32(pointMenu.Y), Convert.ToInt32(NotificationMenu.Width), Convert.ToInt32(NotificationMenu.Height), 0x0400);


        //         NotificationMenu.Show();

        //             App.Input += _WindowInput_Input;

        //             Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen = true;

        //     }
        //     }));
        // }

        // private void _WindowInput_Input(object? sender, RawInputEvent e)
        // {
        //     if (e.Data is not RawInputMouseData mouseData || mouseData.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;

        //     Window[]? Window = App.WindowsIsOpen.Where(result => result.Tag.ToString() is App.GetWindowNotification).ToArray();
        //     Window notificator = Window.Length > 1 || Window.Length == 0 ? throw new NullReferenceException("Окно нотификации не найдено") : Window[0];

        //     if (notificator.IsVisible is false) return;
        //     if (notificator.IsMouseOver) return;
        //     if (notificator.IsVisible)
        //     {
        //         Rectangle iconPos = GetRectanglePosition();

        //         System.Windows.Point cursorPos = CursorFunction.GetCursorPosition();


        //         if (Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width))
        //         {
        //             if (Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)) return;
        //             if (!(Convert.ToInt32(cursorPos.Y) > iconPos.Y & Convert.ToInt32(cursorPos.Y) < (iconPos.Y + iconPos.Size.Height)))
        //             {
        //                 notificator.Hide();
        //                 _iconViewModel.DataCommandsCollection.Clear();
        //                 Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen = false;


        //                 App.Input -= _WindowInput_Input;
        //                 return;

        //             }
        //             return;
        //         };
        //         if (!(Convert.ToInt32(cursorPos.X) > iconPos.X & Convert.ToInt32(cursorPos.X) < (iconPos.X + iconPos.Size.Width)))
        //         {
        //             notificator.Hide();
        //             _iconViewModel.DataCommandsCollection.Clear();

        //             Infrastructure.SharedFunctions.Notificator.NotificationMenuIsOpen = false;

        //             App.Input -= _WindowInput_Input;
        //             return;
        //         }
        //     }

        // }



        // internal static System.Windows.Point GetRectCorrect(Window window)
        // {
        //     System.Windows.Point point = new System.Windows.Point();

        //     Rectangle rectIcon = GetRectanglePosition();

        //     System.Windows.Size elementWindow = GetElementPixelSize(window);

        //     int pixelWidth = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, elementWindow.Width));
        //     int pixelHeight = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, elementWindow.Height));

        //     point.Y = rectIcon.Y - pixelHeight + rectIcon.Size.Height / 2;
        //     point.X = rectIcon.X - pixelWidth;

        //     return point;
        // }


        // internal static System.Windows.Size GetElementPixelSize(UIElement element)
        // {
        //     Matrix transformToDevice;

        //     PresentationSource? source = PresentationSource.FromVisual(element);

        //     //DpiScale aa = VisualTreeHelper.GetDpi(element);



        //     if (source is not null)
        //     {
        //         transformToDevice = source.CompositionTarget.TransformToDevice;
        //     }
        //     else
        //     {
        //         using var source2 = new HwndSource(new HwndSourceParameters());

        //         transformToDevice = source2.CompositionTarget.TransformToDevice;
        //     }

        //     if (element.DesiredSize == new System.Windows.Size())
        //     {
        //         element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        //     }

        //     return (System.Windows.Size)transformToDevice.Transform((Vector)element.DesiredSize);

        // }





        // public void ShutdownAppExecute(object? _) { _notifyIcon?.Dispose(); App.Current.Shutdown(); }


        // public bool IsExecuteShutdownApp(object? _) => true;

        // internal class NotifyIconHelper
        // {

        //     public static Rectangle GetIconRect(NotifyIcon icon)
        //     {
        //         RECT rect = new RECT();
        //         NOTIFYICONIDENTIFIER notifyIcon = new NOTIFYICONIDENTIFIER();

        //         notifyIcon.cbSize = Marshal.SizeOf(notifyIcon);
        //         //use hWnd and id of NotifyIcon instead of guid is needed
        //         notifyIcon.hWnd = GetHandle(icon);
        //         notifyIcon.uID = GetId(icon);

        //         int hresult = Shell_NotifyIconGetRect(ref notifyIcon, out rect);
        //         //rect now has the position and size of icon

        //         return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        //     }

        //     [StructLayout(LayoutKind.Sequential)]
        //     private struct RECT
        //     {
        //         public Int32 left;
        //         public Int32 top;
        //         public Int32 right;
        //         public Int32 bottom;
        //     }

        //     [StructLayout(LayoutKind.Sequential)]
        //     private struct NOTIFYICONIDENTIFIER
        //     {
        //         public Int32 cbSize; // размер структуры
        //         public IntPtr hWnd; //handle родительского окна используемое функцией вызова
        //         public UInt32 uID; //Определенный приложением идентификатор значка уведомления
        //         public Guid guidItem;
        //     }

        //     [DllImport("shell32.dll", SetLastError = true)]
        //     private static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

        //     private static FieldInfo? windowField = typeof(NotifyIcon).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //     private static IntPtr GetHandle(NotifyIcon icon)
        //     {
        //         if (windowField is null) throw new NullReferenceException("Ошибка поиска дескриптора окна иконки");
        //         NativeWindow? window = windowField.GetValue(icon) as NativeWindow;

        //         if (window is null) throw new NullReferenceException("Ошибка поиска дескриптора окна иконки");
        //         return window.Handle;
        //     }

        //     private static FieldInfo? idField = typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //     private static uint GetId(NotifyIcon icon)
        //     {

        //         if (idField is null) throw new NullReferenceException("Не удалось найти закрытое поле идификатора NotifyIcon");

        //         return (uint)idField.GetValue(icon);
        //     }

        // }

    }


}

