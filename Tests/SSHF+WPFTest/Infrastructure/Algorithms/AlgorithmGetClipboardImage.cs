using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SSHF.Infrastructure.Algorithms.Base;
using SSHF.Infrastructure.Interfaces;


namespace SSHF.Infrastructure.Algorithms
{
    internal sealed class AlgorithmGetClipboardImage: BaseAlgorithm
    {
        
        private bool _status = default;

        protected internal override bool IsCheceked => _status;

        protected internal override string? Name => "GetClipboardImage";

        #region Провекрка уловий функции
        internal enum PreChecStartFail
        {
            None,
            Parmeter_Not_StringButTheFunctionCanbeExecuted,
            AppKeyBoardHandler_Is_Null,
            KeyBoardHandler_ReplaceRegisteredFunction_Back_Null
        }
        /// <summary>
        /// Обязательная проверка условий перед первоначальным использованием функции.
        /// <br>
        /// </br>
        /// <br>
        /// Где входной тип <see href="T"/> - <see cref="Tuple{T1, T2}"/>. <see href="T1"/> - массив Vkeys. <see href="T2"/> - <see cref="CancellationToken"/> - допустимо значение <see href="defolt"/>.
        /// </br>
        /// </summary>  
        /// <returns>Резултатат указывает может ли быть выполнена функция <see cref="Start"/></returns>
        protected internal override Task<bool> PreCheckStart<T>(T parameter)
        {
            DictionaryEntry[] PreChecStartFails = new DictionaryEntry[]
            {
                    new DictionaryEntry(PreChecStartFail.Parmeter_Not_StringButTheFunctionCanbeExecuted,"Функция может быть выполнена, но клавиши глобального вызова небыли зарегистрированы"), //0
                    new DictionaryEntry(PreChecStartFail.AppKeyBoardHandler_Is_Null,"Ссылка на класc - обработчик клавиатурного ввода была NULL"),                                                //1
                    new DictionaryEntry(PreChecStartFail.KeyBoardHandler_ReplaceRegisteredFunction_Back_Null,$"{nameof(FuncKeyHandler.FkeyHandler.RegisteredFunction)} вернул {false}"),           //2                                                                                                                //4
            };

            if (parameter is not string nameButton)
            {
                _status = true;
                throw new NullReferenceException().Report(PreChecStartFails[0]);
            }
            try
            {
                if (_status is true)
                {
                    if (App.KeyBoardHandler is null) throw new NullReferenceException().Report(PreChecStartFails[1]);

                    if (App.KeyBoardHandler.ReplaceRegisteredFunction(Name, nameButton, new Action(() => { Start<BitmapSource, object>(new object()); })) is false)
                    {
                        throw new NullReferenceException().Report(PreChecStartFails[2]);
                    }
                }
                if (_status is false)
                {
                    if (App.KeyBoardHandler is null) throw new NullReferenceException().Report(PreChecStartFails[1]);

                    App.KeyBoardHandler.RegisterAFunction(Name, nameButton, new Action(() => { Start<BitmapSource, object>(new object()); }), true);

                    _status = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains(PreChecStartFail.Parmeter_Not_StringButTheFunctionCanbeExecuted) is true)
                {
                    _status = true;
                    throw;
                }
                else
                {
                    _status = false;
                    throw;
                }
            }
            return Task.FromResult(true);
        }
        #endregion

        #region Основное тело фукции
        bool isProcessing = false;
        enum StartFail
        {
            None,
            The_Function_Is_Not_Registered,
            It_Is_Not_Possible_To_Perform_a_Startup_Before_The_Current_Operation_is_Complete,
            Clipboard_Empty,
            The_Output_Type_Is_Not_BitmapSource,
            OperationCanceled
        }
        /// <summary>
        /// Запуск алогоритма получения изображения из буффера обмена.
        /// <br>
        /// </br>
        /// <br>
        /// Где <see href="T"/> - <see cref="BitmapSource"/> обозначает выходной результат.
        ///  <br>
        /// Где <see href="T2"/> - <see cref="Nullable{T}"/> - <see href="T"/> <see cref="CancellationToken"/>
        /// </br>
        /// </br>
        /// </summary>  
        /// <returns>Возращает изображение из буффера обменна в формате  <see cref="BitmapSource"/>.</returns>
        protected internal override Task<T> Start<T, T2>(T2 parameter)
        {
            var StartFunctionFails = GetLazzyDictionaryFails
            (
              new KeyValuePair<StartFail, string>(StartFail.The_Function_Is_Not_Registered, $"Опрерация не зарегистрирована. Вызовите мотод {nameof(PreCheckStart)} перед первым использованием"),           //0
              new KeyValuePair<StartFail, string>(StartFail.It_Is_Not_Possible_To_Perform_a_Startup_Before_The_Current_Operation_is_Complete, "Невозможно выполнить запуск до завершения текущей операции"),//1
              new KeyValuePair<StartFail, string>(StartFail.Clipboard_Empty, "В буффере обмена остувует изображение"),                                                                                      //2
              new KeyValuePair<StartFail, string>(StartFail.The_Output_Type_Is_Not_BitmapSource, $"Тип {nameof(T)} долженя вляется {typeof(BitmapSource)}"),                                                //3
              new KeyValuePair<StartFail, string>(StartFail.OperationCanceled, $"Операция операция получила запрос отмены через ... и была отмененна")
            );         
            try
            {
                if (Equals(typeof(T), typeof(BitmapSource)) is not true) throw new InvalidOperationException().Report(StartFunctionFails.Value[3]);
                if (_status is false) throw new InvalidOperationException().Report(StartFunctionFails.Value[0]);
                if (isProcessing is true) throw new InvalidOperationException().Report(StartFunctionFails.Value[1]);

                isProcessing = true;
                try
                {
                    if (GetClipboardImage() is not T source) throw new NullReferenceException().Report(StartFunctionFails.Value[2]);
                    return Task.FromResult(source);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                isProcessing = false;
            }
        }
        #endregion

        #region Вспомогательные методы
        internal enum GetClipboardImageFail
        {
            None,
            OperationCanceled
        }
        private BitmapSource? GetClipboardImage(CancellationToken? token = null)
        {
            var GetClipboardImageFails = GetLazzyDictionaryFails
            (
             new KeyValuePair<GetClipboardImageFail, string>(GetClipboardImageFail.OperationCanceled, $"Операция была отменена через {nameof(token)}") //0           
            );

            CancellationToken Cancel = token ??= default;

            if (Cancel.IsCancellationRequested is true) throw new OperationCanceledException(Cancel).Report(GetClipboardImageFails.Value[0]);

            BitmapSource? ReturnValue = null;
            Thread STAThread = new Thread(() =>
            {
                if (Clipboard.ContainsImage() is true) ReturnValue = Clipboard.GetImage();              
                else ReturnValue = null;              
            });

            if (Cancel.IsCancellationRequested is true) throw new OperationCanceledException(Cancel).Report(GetClipboardImageFails.Value[0]);

            STAThread.SetApartmentState(ApartmentState.STA);          
            STAThread.Start();
            STAThread.Join();

            RenderOptions.SetBitmapScalingMode(ReturnValue, BitmapScalingMode.NearestNeighbor);
            return ReturnValue;
        }
        #endregion                   

    }
}
