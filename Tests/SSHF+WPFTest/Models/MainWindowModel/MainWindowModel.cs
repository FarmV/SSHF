using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        #region Работа с курсором

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
        static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);
      
        public static Point GetWindosPosToCursor()
        {
            return App.Current.MainWindow.TranslatePoint(GetCursorXY(), App.Current.MainWindow);
        }

        private static Point GetCursorXY()
        {

            var transform = PresentationSource.FromVisual(App.Current.MainWindow).CompositionTarget.TransformFromDevice;
            Point mouse = transform.Transform(MainWindowModel.GetCursorPosition());
            return mouse;
         
        }

        #endregion

        private static bool _FlagRefreshCurrentWindow = false;
     
        private static void RefreshWindow()
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.MainWindowHandle;
            _FlagRefreshCurrentWindow = true;
            while (_FlagRefreshCurrentWindow)
            {
                POINT cursorPoint = new POINT();
                GetCursorPos(out cursorPoint);                                
                SetWindowPos(currentProcessHandle, -1, cursorPoint.X+7, cursorPoint.Y+7, 200, 200, 0x0400);             
            }
        }

        public static void ExecuteRefreshWindowOn(object? parameter)
        {
            RefreshWindow();
        }

        public static bool CanExecuteRefreshWindowOn(object? parameter)
        {
            return _FlagRefreshCurrentWindow is false;
        }

        public static void CommandExecuteRefreshWindowOFF(object? parameter)
        {
            _FlagRefreshCurrentWindow = false;
        }

        public static bool CanCommandExecuteRefreshWindowOFF(object? parameter)
        {
            return _FlagRefreshCurrentWindow is true;
        }

    }
}
