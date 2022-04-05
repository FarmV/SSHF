using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class CursorFunctions
    {
       

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        internal static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }



        internal static Point GetCursorXY(UIElement element)
        {

            var transform = PresentationSource.FromVisual(element).CompositionTarget.TransformFromDevice;
            Point mouse = transform.Transform(CursorFunctions.GetCursorPosition());
            return mouse;

        }


        internal static Point GetWindosPosToCursor(UIElement element)
        {
            return App.Current.MainWindow.TranslatePoint(CursorFunctions.GetCursorXY(App.Current.MainWindow), element);
        }


    }
}
