using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSHF.Models.NotifyIconModel
{
    internal class NotifyIconModel
    {

        //public static Rectangle GetIconRect(NotifyIcon icon)
        //{
        //    RECT rect = new RECT();
        //    NOTIFYICONIDENTIFIER notifyIcon = new NOTIFYICONIDENTIFIER();

        //    notifyIcon.cbSize = Marshal.SizeOf(notifyIcon);
        //    //use hWnd and id of NotifyIcon instead of guid is needed
        //    notifyIcon.hWnd = GetHandle(icon);
        //    notifyIcon.uID = GetId(icon);

        //    int hresult = Shell_NotifyIconGetRect(ref notifyIcon, out rect);
        //    //rect now has the position and size of icon

        //    return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //private struct RECT
        //{
        //    public Int32 left;
        //    public Int32 top;
        //    public Int32 right;
        //    public Int32 bottom;
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //private struct NOTIFYICONIDENTIFIER
        //{
        //    public Int32 cbSize;
        //    public IntPtr hWnd;
        //    public Int32 uID;
        //    public Guid guidItem;
        //}

        //[DllImport("shell32.dll", SetLastError = true)]
        //private static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

        //private static FieldInfo? windowField =
        //    typeof(NotifyIcon).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //private static IntPtr GetHandle(NotifyIcon icon)
        //{
           
            
        //    if (windowField == null) throw new InvalidOperationException("[Useful error message]");
        //    NativeWindow window = windowField.GetValue(icon) as NativeWindow;

        //    if (window == null) throw new InvalidOperationException("[Useful error message]");  // should not happen?
        //    return window.Handle;
        //}

        //private static FieldInfo? idField = typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //private static int GetId(NotifyIcon icon)
        //{
        //    if (idField == null) throw new InvalidOperationException("[Useful error message]");
        //    return (int)idField.GetValue(icon);
        //}

    }


}

