using System.Runtime.InteropServices;
using System.Windows;

namespace FVH.SSHF.Infrastructure
{
    internal partial class Win32Cursor
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public static implicit operator Point(POINT point) => new Point(point.X, point.Y);            
        }
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetCursorPos(out POINT lpPoint);
        internal static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }
        internal static Point GetCursorXY(UIElement element)
        {
            System.Windows.Media.Matrix transform = PresentationSource.FromVisual(element).CompositionTarget.TransformFromDevice;
            return transform.Transform(GetCursorPosition());
        }
        internal static Point GetWindosPosToCursorWPF(Window window,UIElement element) => window.TranslatePoint(Win32Cursor.GetCursorXY(window), element);        
    }
}
