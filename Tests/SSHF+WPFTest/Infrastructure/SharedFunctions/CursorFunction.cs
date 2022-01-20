using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF.Infrastructure.SharedFunctions
{
    internal class CursorFunction
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
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }



        public static Point GetCursorXY(UIElement element)
        {

            var transform = PresentationSource.FromVisual(element).CompositionTarget.TransformFromDevice;
            Point mouse = transform.Transform(CursorFunction.GetCursorPosition());
            return mouse;

        }


        public static Point GetWindosPosToCursor(UIElement element)
        {
            return App.Current.MainWindow.TranslatePoint(CursorFunction.GetCursorXY(App.Current.MainWindow), element);
        }


    }
}
