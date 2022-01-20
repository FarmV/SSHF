using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using FuncKeyHandler;

using SSHF.ViewModels;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            _ViewModel = ViewModel;
            RegisterFunctions();

        }





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

        #region Обновление окна



        public async void ExecuteRefreshWindowOn(object? parameter) => await Task.Run(() =>
        {
                               
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.MainWindowHandle;
            _ViewModel.RefreshWindow = true;
            while (_ViewModel.RefreshWindow)
            {
                POINT cursorPoint = new POINT();
                GetCursorPos(out cursorPoint);
                SetWindowPos(currentProcessHandle, -1, cursorPoint.X - (1920 / 2) + 15, cursorPoint.Y - (1080 / 2), 1920, 1080, 0x0400);
            }
            
        });


        public bool CanExecuteRefreshWindowOn(object? parameter) => _ViewModel.RefreshWindow is false;
        public void CommandExecuteRefreshWindowOFF(object? parameter) => _ViewModel.RefreshWindow = false;
        public bool CanCommandExecuteRefreshWindowOFF(object? parameter)
        {
            return _ViewModel.RefreshWindow is true;
        }

        #endregion

        #region Обработчик клавиатурного вввода

        public readonly FkeyHandler _FuncAndKeyHadler = new FkeyHandler("+");



        void RegisterFunctions()
        {
            _FuncAndKeyHadler.RegisterAFunction("CanCommandExecuteRefreshWindowOFF", "KEY_1 + KEY_2 + KEY_3", new Action(() => {_ViewModel.RefreshOFF.Execute(new object()); }), true);
           
        }

        





        #endregion
    }
}
