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
    internal class FunctionGetClipboardImage: Freezable, IActionFunction
    {
        protected override Freezable CreateInstanceCore()
        {
            return new FunctionGetClipboardImage();
        }

        public string Name => "GetClipboardImage";

        public event Action<object?>? Сompleted;

        private bool _status = default;
        public bool Enable
        {
            get => _status;
        }

        public Task<Tuple<bool, string>> CheckAndRegistrationFunction(object? parameter = null)
        {

            if (parameter is not string nameButton)
            {
                _status = true;
                return Task.FromResult(Tuple.Create(true, "Функция может быть выполнена, но клавиши глобального вызова небыли зарегистрированы"));
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

                    App.KeyBoardHandler.RegisterAFunction(Name, nameButton, new Action(() => { StartFunction(); }), true);

                    _status = true;
                }
            }
            catch (Exception)
            {
                _status = false;
                Task.FromResult(Tuple.Create(false, "Произошла ошибка в класе регистрации регистрации клавиш"));
            }

            return Task.FromResult(Tuple.Create(true, "Функция и клавиши успешно зарегистрированны"));
        }


        bool isProcessing = false;
        public async Task<Tuple<bool, object?, string>> StartFunction(object? parameter = null)
        {

            if (_status is false)
            {   
                var resFailRegistration = Tuple.Create<bool, object?, string>(false, null, $"Опрерация не зарегистрирована. Вызовите мотод {nameof(CheckAndRegistrationFunction)}");


                return resFailRegistration;
            }
            if (isProcessing is true) return Tuple.Create<bool, object?, string>(false, null, $"Не завершилась прошлая операция");

            isProcessing = true;

            Tuple<bool, object?, string> result = await StartAlgorithm();

            if (result.Item1 is false) return Tuple.Create<bool, object?, string>(false, result.Item2, result.Item3);

            isProcessing = false;

            return Tuple.Create<bool, object?, string>(true, result.Item2, result.Item3);
        }

        public bool СancelFunction(object? parameter = null)
        {
            return false;
        }


        static async Task<Tuple<bool, object?, string>> StartAlgorithm()
        {

            if (GetClipboardImage() is not BitmapSource source) return Tuple.Create<bool, object?, string>(false, null, "Буфер обмена пустой");

            IImageOperations imageOperations = new SSHF.Infrastructure.ImplementingInterfaces.ImageOperations.ImageOperations();

            Tuple<bool, object?, string> TaskConvert = await imageOperations.ConvetImage(
            IImageOperations.ImageType.SystemWindowsMediaImagingBitmapSource,
            IImageOperations.ImageType.SystemWindowsMediaImagingBitmapImage,
            source);

            if (TaskConvert.Item1 is false) return Tuple.Create<bool, object?, string>(false, TaskConvert.Item2, TaskConvert.Item3);

            return Tuple.Create<bool, object?, string>(true, TaskConvert.Item2, TaskConvert.Item3);
        }

        #region Вспомогательные методы.
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

        #endregion
    }
}
