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

using System.Windows.Forms;

using static SSHF.Infrastructure.SharedFunctions.CursorFunction;
using SSHF.ViewModels.MainWindowViewModel;
using SSHF.Models.NotifyIconModel;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;
       // readonly System.Windows.Forms.NotifyIcon? _Icon;
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            using (_ViewModel = ViewModel)
            RegisterFunctions();
            
        }


        #region Обновление окна
        static IntPtr MainWindowHandle => new Func<IntPtr>(() => { Process currentProcess = Process.GetCurrentProcess(); return currentProcess.MainWindowHandle; }).Invoke();
        POINT _CursorPoint = default;
        readonly POINT _PositionShift = new POINT
        {
            X = (1920 / 2),
            Y = (1080 / 2)
        };

        public async void RefreshWindowOnExecute(object? parameter) => await Task.Run(() =>
        {
            _ViewModel.RefreshWindow = true;
            while (_ViewModel.RefreshWindow)
            {
                GetCursorPos(out _CursorPoint);
                WindowFunction.SetWindowPos(MainWindowHandle, -1, _CursorPoint.X - _PositionShift.X, _CursorPoint.Y - _PositionShift.Y, 1920, 1080, 0x0400); // todo Отвязать от разрешения. Узнать как комибинировать флаги.
            }

          

        });
        public bool IsRefreshWindowOn(object? parameter) => _ViewModel.RefreshWindow is false;



        public void RefreshWindowOffExecute(object? parameter) => _ViewModel.RefreshWindow = false;
        public bool IsExecuteRefreshWindowOff(object? parameter)
        {
            return _ViewModel.RefreshWindow is true;
        }

        #endregion

        #region Вызов окна Help

        public void HelpInvoce(object? parameter) => new Action(() => { }).Invoke();
        public bool IsExecuteHelp(object? parameter)
        {
            return true;

            
        }

        #endregion


        #region Обработчик клавиатурного вввода

        public readonly FkeyHandler _FuncAndKeyHadler = new FkeyHandler(App._GlobaKeyboardHook, "+");



        void RegisterFunctions()
        {
            _FuncAndKeyHadler.RegisterAFunction("RefreshWindowOFF", "KEY_1 + KEY_2 + KEY_3", new Action(() => {_ViewModel.RefreshOffCommand.Execute(new object()); }), true);           
        }


        #endregion


        #region Выбор файла изображения

        public void SelectFileExecute(object? parameter)
        {
            if (DialogFile.OpenFile("Выбор изображения", out string? filePath) is false) return;
           
            if(filePath is null) return;
            
            FileInfo file = new FileInfo(filePath);

            BitmapImage? image = IntegratingImages.SetImageToMemoryFromDrive(new Uri(file.FullName, UriKind.Absolute));

            if (image is null)
            {
                System.Windows.Forms.MessageBox.Show($"Не удалось обработать файл изображения.{Environment.NewLine}Проверте расширение файла.","Ошибка",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };         

            _ViewModel.Image = image;

           // BitmapImage? scaleImage = IntegratingImages.ImageScale(image);
            //if (scaleImage is null) throw new Exception("Scale?");
            _ViewModel.ImageOpacity = image;


        }
        
        public bool IsExecuteSelectFile(object? parameter)
        {
            return true;
        }


        #endregion


    }
}
