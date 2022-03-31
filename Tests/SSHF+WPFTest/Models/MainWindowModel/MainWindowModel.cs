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
using System.Collections.Generic;
using SSHF.ViewModels.NotifyIconViewModel;
using System.Linq;
using Linearstar.Windows.RawInput;
using SSHF.Infrastructure.Algorithms;
using SSHF.Infrastructure.Interfaces;
using SSHF.Infrastructure.Algorithms.Base;
using System.Threading;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;  
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            using (_ViewModel = ViewModel)

                if (App.IsDesignMode is not true)
                {
                    RegisterFunctions();
                }

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

                WindowFunction.SetWindowPos(MainWindowHandle, -1, _CursorPoint.X, _CursorPoint.Y, 10, 10, 0x0400 | 0x0001);
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

        // public readonly FkeyHandler _FuncAndKeyHadler = new FkeyHandler(App._GlobaKeyboardHook, "+");


        
        void RegisterFunctions()
        {
            KeyboardKeyCallbackFunction callback = KeyboardKeyCallbackFunction.GetInstance();
            string DeeplDirectory = @"C:\Users\Vikto\AppData\Local\DeepL\DeepL.exe";
            string ScreenshotReaderDirectory = @"D:\_MyHome\Требуется сортировка барахла\Portable ABBYY Screenshot Reader\ScreenshotReader.exe";
            var keyCombianteionGetTranslate = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
            {
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.KEY_1,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.KEY_2,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.KEY_3
            };
            callback.AddCallBackTask(keyCombianteionGetTranslate, () => new Task(async() => 
            {
                AlgorithmGetTranslateAbToDepl instance = await AlgorithmGetTranslateAbToDepl.GetInstance(DeeplDirectory, ScreenshotReaderDirectory);
                try
                {
                    await instance.Start<string, bool>(false);
                }
                catch (Exception) { }
            }));

            var keyCombianteionGetClipboardImageAndRefresh = new Infrastructure.Algorithms.Input.Keybord.Base.VKeys[]
            {
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.LWIN,
                Infrastructure.Algorithms.Input.Keybord.Base.VKeys.SHIFT,
                 Infrastructure.Algorithms.Input.Keybord.Base.VKeys.KEY_A
            };
            callback.AddCallBackTask(keyCombianteionGetClipboardImageAndRefresh, () => new Task(async () =>
            {
                AlgorithmGetClipboardImage instance = new AlgorithmGetClipboardImage();
                try
                {
                    BitmapSource bitSor = await instance.Start<BitmapSource, object>(new object());

                    using MemoryStream createFileFromImageBuffer = new MemoryStream();

                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitSor));
                    encoder.Save(createFileFromImageBuffer);

                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = createFileFromImageBuffer;
                    image.EndInit();
                    image.Freeze();

                    _ViewModel.Image = image;
                    _ViewModel.ImageOpacity = image;



                }
                catch (Exception) { }
            }));




        }
        #endregion
































        // todo перенести в нужную модель
        #region Перенести в нужную модель


        #region Выбор файла изображения

        public void SelectFileExecute(object? parameter)
        {
            if (DialogFile.OpenFile("Выбор изображения", out string? filePath) is false) return;

            if (filePath is null) return;

            FileInfo file = new FileInfo(filePath);

            BitmapImage? image = IntegratingImages.SetImageToMemoryFromDrive(new Uri(file.FullName, UriKind.Absolute));

            if (image is null)
            {
                System.Windows.Forms.MessageBox.Show($"Не удалось обработать файл изображения.{Environment.NewLine}Проверте расширение файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };

            _ViewModel.Image = image;

            _ViewModel.ImageOpacity = image;

        }

        public bool IsExecuteSelectFile(object? parameter)
        {
            return true;
        }


        #endregion

        #region Вызов нотификатора


        public void NotificatorExecute(object? _)
        {
            List<NotificatorViewModel.DataModelCommands> commands = new List<NotificatorViewModel.DataModelCommands>
            {
                new NotificatorViewModel.DataModelCommands("Выбрать файл", _ViewModel.SelectFileCommand),
                new NotificatorViewModel.DataModelCommands("Сохранить файл", _ViewModel.InvoceSaveFileDialogCommand)
            };

            //Notificator.SetCommand(commands);
        }
        public bool IsExecuteInvoceNotificator(object? _)
        {
            return true;
        }
        #endregion
        #region Вызов сохранения

        public void SaveFileDialogExecute(object? _)
        {
            if (DialogFile.SaveFileDirectory("Выбирете директория для сохранения") is not string directory) return;
            if (_ViewModel.ImageOpacity is null) throw new NullReferenceException("_ViewModel.ImageOpacit is NULL");

            if (IntegratingImages.SafeImage(new Uri(directory), _ViewModel.ImageOpacity) is false)
            {
                System.Windows.MessageBox.Show("Функци сохранения изображения выернула ошибку", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }





        }
        public bool IsExecuteSaveFileDialog(object? _)
        {
            return true;
        }

        #endregion
        #endregion
    }





}
