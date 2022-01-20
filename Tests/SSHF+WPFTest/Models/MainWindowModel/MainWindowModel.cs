using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FuncKeyHandler;

using SSHF.Infrastructure.SharedFunctions;
using SSHF.ViewModels;

using static SSHF.Infrastructure.SharedFunctions.CursorFunction;

namespace SSHF.Models.MainWindowModel
{
    internal class MainWindowModel
    {
        readonly MainWindowViewModel _ViewModel;
        readonly NotificatioIcon _Icon;
        public MainWindowModel(MainWindowViewModel ViewModel)
        {
            _ViewModel = ViewModel;
            RegisterFunctions();
            _Icon = new NotificatioIcon();
        }


        #region Обновление окна
        IntPtr _MainWindowHandle => new Func<IntPtr>(() => { Process currentProcess = Process.GetCurrentProcess(); return currentProcess.MainWindowHandle; }).Invoke();
        POINT _CursorPoint = default;
        readonly POINT _PositionShift = new POINT
        {
             X = (1920 / 2) + 15,
             Y = (1080 / 2)
        };
       
    public async void ExecuteRefreshWindowOn(object? parameter) => await Task.Run(() =>
        {                                 
            _ViewModel.RefreshWindow = true;
            while (_ViewModel.RefreshWindow)
            {                
                GetCursorPos(out _CursorPoint);
                WindowFunction.SetWindowPos(_MainWindowHandle, -1, _CursorPoint.X - _PositionShift.X, _CursorPoint.Y - _PositionShift.Y, 1920, 1080, 0x0400);
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

        #region Интеграция изображений

        #region Извлечение изображения из буфера обмена
        /// <returns>Возращает изображание, помещённое в память, из буфера обмена. Или пустую ссылку.</returns>
        BitmapSource? GetBufferImage()
        {
          
            BitmapSource? image = Clipboard.GetImage();

            if (image is null) return image;

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            return image;
        }
        #endregion

        #region Изображения в память(инициализация)
        /// <summary>
        ///     <br>Путь извлечения изображения <see cref="Uri"/> <paramref name="path"/></br>
        /// </summary>
        /// <returns>Возращает изображание, помещённое в память, или вызывает исключение.</returns>
        BitmapImage SetImageToMemoryFromDrive(Uri path)
        {

            if (System.IO.File.Exists(path.AbsolutePath))
            {
                BitmapImage Image = new BitmapImage();              // Инициализаци что бы файл был в памяти и не заянят
                Image.BeginInit();
                Image.UriSource = path;
                Image.CacheOption = BitmapCacheOption.OnLoad;
                Image.EndInit();

                return Image;
            }
            else throw new ArgumentException("SetImage Fail", nameof(path));
        }
        #endregion


        #region Сохранение изображения на диск
        /// <summary>
        ///     <br>Путь для сохранения <see cref="Uri"/> <paramref name="path"/></br>
        ///     <br> Изображение для сохранения <see cref="BitmapSource"/> <paramref name="Image"/></br>
        /// </summary>
        /// <returns>Успешность операции.</returns>
        bool SafeImage(Uri path, BitmapSource Image)
        {
            try
            {
              using (FileStream createFileFromImageBuffer = new FileStream(path.AbsolutePath, FileMode.OpenOrCreate))
              {
                  BitmapEncoder encoder = new PngBitmapEncoder();
                  encoder.Frames.Add(BitmapFrame.Create(Image));
                  encoder.Save(createFileFromImageBuffer);
              }
              return true;
            }
            catch (Exception){ return false; }
        }
        #endregion

        #endregion
    }
}
