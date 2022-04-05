﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels;
using SSHF.Infrastructure;

using System.Windows.Forms;

using static SSHF.Infrastructure.SharedFunctions.CursorFunctions;
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
using SSHF.Infrastructure.Algorithms.KeyBoards.Base;

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

        public void RefreshWindowOnExecute(object? parameter) => Task.Run(() =>
        {
            _ViewModel.RefreshWindow = true;
            while (_ViewModel.RefreshWindow)
            {
                GetCursorPos(out _CursorPoint);

                WindowFunctions.RefreshWindowPositin.SetWindowPos(MainWindowHandle, -1, _CursorPoint.X, _CursorPoint.Y, 10, 10, 0x0400 | 0x0001);
            }
        }).ConfigureAwait(false);
        public bool IsRefreshWindowOn(object? parameter) => _ViewModel.RefreshWindow is false;


        public void RefreshWindowOffExecute(object? parameter) => _ViewModel.RefreshWindow = false;
        public bool IsExecuteRefreshWindowOff(object? parameter) => _ViewModel.RefreshWindow is true;


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



        private Task RegisterFunctions() => Task.Run(() =>
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
            callback.AddCallBackTask(keyCombianteionGetTranslate, () => new Task(async () =>
            {
                AlgorithmGetTranslateAbToDepl instance = await AlgorithmGetTranslateAbToDepl.GetInstance(DeeplDirectory, ScreenshotReaderDirectory);
                try
                {
                    await instance.Start<string, bool>(false);
                }
                catch (Exception) { }
            })).ConfigureAwait(false);

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

                    using MemoryStream createFileFromImageBuffer = new MemoryStream();         //todo переехать в интерфейс конвертации изображений
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    BitmapFrame ccc = BitmapFrame.Create(bitSor);
                    encoder.Frames.Add(BitmapFrame.Create(bitSor));
                    encoder.Save(createFileFromImageBuffer);
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = createFileFromImageBuffer;
                    image.EndInit();
                    image.Freeze();

                    await App.WindowsIsOpen[App.GetMyMainWindow].Value.InvokeAsync(() =>
                    {
                        _ViewModel.Image = image;
                        App.WindowsIsOpen[App.GetMyMainWindow].Key.Height = image.Height;
                        App.WindowsIsOpen[App.GetMyMainWindow].Key.Width = image.Width;
                        App.WindowsIsOpen[App.GetMyMainWindow].Key.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        App.WindowsIsOpen[App.GetMyMainWindow].Key.Show();
                    });
                    CancellationTokenSource tokenSource = new CancellationTokenSource();

                    void MouseInput(object? sender, Infrastructure.Algorithms.Input.RawInputEvent e)
                    {
                        if (e.Data is RawInputKeyboardData)
                        {
                            if (KeyBordBaseRawInput.PresKeys.Contains(Infrastructure.Algorithms.Input.Keybord.Base.VKeys.CONTROL) is true)
                            {
                                tokenSource.Cancel();
                                App.Input -= MouseInput;
                                return;
                            }
                        }
                        if (e.Data is not RawInputMouseData data || data.Mouse.Buttons is Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None) return;
                        else
                        {
                            tokenSource.Cancel();
                            App.Input -= MouseInput;
                        }

                    }
                    _ = Task.Run(() =>
                    {
                        App.Input += MouseInput;
                    }).ConfigureAwait(false);

                    await SSHF.Infrastructure.SharedFunctions.WindowFunctions.RefreshWindowPositin.RefreshWindowPosCursor(App.WindowsIsOpen[App.GetMyMainWindow].Key, tokenSource.Token);
                }
                catch (Exception) { }
            })).ConfigureAwait(false);

        });


        #endregion




        // todo перенести в нужную модель
        #region Перенести в нужную модель


        #region Выбор файла изображения

        public void SelectFileExecute(object? parameter)
        {
            if (DialogFileFunctions.OpenFile("Выбор изображения", out string? filePath) is false) return;

            if (filePath is null) return;

            FileInfo file = new FileInfo(filePath);

            BitmapImage? image = ImagesFunctions.SetImageToMemoryFromDrive(new Uri(file.FullName, UriKind.Absolute));

            if (image is null)
            {
                System.Windows.Forms.MessageBox.Show($"Не удалось обработать файл изображения.{Environment.NewLine}Проверте расширение файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };

            _ViewModel.Image = image;

            _ViewModel.ImageOpacity = image;

        }
        public bool IsExecuteSelectFile(object? parameter) => true;
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
            if (DialogFileFunctions.SaveFileDirectory("Выбирете директория для сохранения") is not string directory) return;
            if (_ViewModel.ImageOpacity is null) throw new NullReferenceException("_ViewModel.ImageOpacit is NULL");

            if (ImagesFunctions.SafeImage(new Uri(directory), _ViewModel.ImageOpacity) is false)
            {
                System.Windows.MessageBox.Show("Функци сохранения изображения выернула ошибку", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public bool IsExecuteSaveFileDialog(object? _) => true;
        #endregion
        #endregion
    }





}