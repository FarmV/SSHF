using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SSHF.Infrastructure.Interfaces;


namespace SSHF.Infrastructure.Algorithms
{

    //<see href = "http://stackoverflow.com" > HERE </ see >

    /// <summary>
    /// <br> <see cref="CheckAndRegistrationFunction"/> Ожидает получение комбинации клавиш глобального вызова в формате <see cref="FuncKeyHandler.FkeyHandler.VKeys"/>, c разделителем "+" между клавишами. Клавиши не обязательны.</br>
    /// </summary>
    internal class FunctionScreenShot: Freezable, IActionFunction
    {
        protected override Freezable CreateInstanceCore()
        {
            return new FunctionScreenShot();
        }

        public string Name => "ScreenShot";

        public event Action<object?>? Сompleted;

        private bool _status = default;
        public bool Enable
        {
            get => _status;
        }

        public Tuple<bool, string> CheckAndRegistrationFunction(object? parameter = null)
        {

            if (parameter is not string nameButton)
            {
                _status = true;
                return Tuple.Create(true, "Функция может быть выполнена, но клавиши глобального вызова небыли зарегистрированы");
            }
            try
            {
                if (_status is true)
                {
                    if (App.KeyBoardHandler is null) throw new NullReferenceException("App.KeyBoarHandle is NULL");

                    if (App.KeyBoardHandler.ReplaceRegisteredFunction(Name, nameButton, new Action(() => { StartFunction(); })) is false)
                    {
                        throw new NullReferenceException("KeyBoardHandler.ReplaceRegisteredFunction вернул false");
                    }
                }
                if (_status is false)
                {
                    if (App.KeyBoardHandler is null) throw new NullReferenceException("App.KeyBoarHandle is NULL");

                    App.KeyBoardHandler.RegisterAFunction("Name", nameButton, new Action(() => { StartFunction(); }), true);

                    _status = true;
                }
            }
            catch (Exception)
            {
                _status = false;
                Tuple.Create(false, "Произошла ошибка в класе регистрации регистрации клавиш");
            }

            return Tuple.Create(true, "Функция и клавиши успешно зарегистрированны");
        }

        bool isProcessing = false;
        public bool StartFunction(object? parameter = null)
        {
            if (_status is false) return false;

            isProcessing = true;
            StartAlgorithm();

            return true;
        }

        public bool СancelFunction(object? parameter = null)
        {
            return false;
        }





        bool StartAlgorithm()
        {

            if (GetClipboardImage() is not BitmapSource source) return false;


            using (FileStream createFileFromImageBuffer = new FileStream(PathOriginalScreenshot, FileMode.OpenOrCreate))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(createFileFromImageBuffer);
            }


            _ImageScreen = source;


            Сompleted?.Invoke(true);
            isProcessing = false;
            return true;

        }

        static BitmapSource? GetClipboardImage()
        {
            BitmapSource? ReturnValue = null;
            Thread STAThread = new Thread(() =>
            {
                if (Clipboard.ContainsImage() is true)
                {
                    ReturnValue = Clipboard.GetImage();
                }
                else
                {
                    ReturnValue = null;
                }
            });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            RenderOptions.SetBitmapScalingMode(ReturnValue, BitmapScalingMode.NearestNeighbor);
            return ReturnValue;
        }


        private BitmapImage? _ImageScreen;


        public BitmapImage? Image
        {
            get
            {
                if (_status is false) throw new InvalidOperationException("Зарегистрируйте функцию");
                return _ImageScreen;
            }
            set
            {
                _ImageScreen = value;
            }
        }









    }
}
