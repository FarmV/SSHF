using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF_WPFTest.Models.MainWindowModel
{
    internal class MainWindowModel
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

        private static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return lpPoint;
        }
                  
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
       
        private static Point GetCursorXY()
        {
            var transform = PresentationSource.FromVisual(App.Current.MainWindow).CompositionTarget.TransformFromDevice;
            Point mouse = transform.Transform(MainWindowModel.GetCursorPosition());
            return mouse;
            
        }

        public static Point GetWindosPosToCursor()
        {
            return App.Current.MainWindow.TranslatePoint(GetCursorXY(), App.Current.MainWindow);
        }


        private static bool CursorPosOn = false;



        private static void RefreshWindow()
        {
            CursorPosOn = true;
            while (CursorPosOn)
            {
                Point point = GetWindosPosToCursor();
                App.Current.MainWindow.Top = point.Y;
                App.Current.MainWindow.Left = point.X;
            }
        }
         
        public static void CommandExecuteRefreshWindowOn(object? parameter)
        {
            RefreshWindow();
            //MessageBox.Show("Привет " + Convert.ToString(parameter));
        }

        public static bool CanCommandExecuteRefreshWindowOn(object? parameter)
        {

            return CursorPosOn is false;
            //return TextBox1.Text != string.Empty;
        }

        public static void CommandExecuteRefreshWindowOFF(object? parameter)
        {
            CursorPosOn = false;
            //MessageBox.Show("Привет " + Convert.ToString(parameter));
        }

        public static bool CanCommandExecuteRefreshWindowOFF(object? parameter)
        {

            return CursorPosOn is true;
            //return TextBox1.Text != string.Empty;
        }

    }
}
