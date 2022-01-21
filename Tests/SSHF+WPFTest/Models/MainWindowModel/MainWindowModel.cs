using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FuncKeyHandler;

using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels;
using SSHF.Infrastructure;

using static SSHF.Infrastructure.SharedFunctions.CursorFunction;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;
        readonly NotificatioIcon _Icon;
        readonly Initialize _Initialize;
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            _ViewModel = ViewModel;
            RegisterFunctions();
            _Initialize = new Initialize();
            _Icon = new NotificatioIcon();
        }


        #region Обновление окна
        static IntPtr MainWindowHandle => new Func<IntPtr>(() => { Process currentProcess = Process.GetCurrentProcess(); return currentProcess.MainWindowHandle; }).Invoke();
        POINT _CursorPoint = default;
        readonly POINT _PositionShift = new POINT
        {
             X = (1920 / 2) + 15,
             Y = (1080 / 2)
        };
       
        public async void RefreshWindowOnExecute(object? parameter) => await Task.Run(() =>
        {                                 
            _ViewModel.RefreshWindow = true;
            while (_ViewModel.RefreshWindow)
            {                
                GetCursorPos(out _CursorPoint);
                WindowFunction.SetWindowPos(MainWindowHandle, -1, _CursorPoint.X - _PositionShift.X, _CursorPoint.Y - _PositionShift.Y, 1920, 1080, 0x0400);
            }
            
        });


        public bool IsRefreshWindowOn(object? parameter) => _ViewModel.RefreshWindow is false;
        public void RefreshWindowOffExecute(object? parameter) => _ViewModel.RefreshWindow = false;
        public bool IsExecuteRefreshWindowOff(object? parameter)
        {
            return _ViewModel.RefreshWindow is true;
        }

        #endregion

        #region Обработчик клавиатурного вввода

        public readonly FkeyHandler _FuncAndKeyHadler = new FkeyHandler(Initialize._GlobaKeyboardHook, "+");



        void RegisterFunctions()
        {
            _FuncAndKeyHadler.RegisterAFunction("CanCommandExecuteRefreshWindowOFF", "KEY_1 + KEY_2 + KEY_3", new Action(() => {_ViewModel.RefreshOFF.Execute(new object()); }), true);           
        }







        #endregion

    }
}
