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
    internal class AlgorithmGetClipboardImage: BaseAlgorithm
    {
        protected internal override bool IsCheceked => isProcessing;

        protected internal override string Name => "GetClipboardImage";


        #region Основное тело фукции
        bool isProcessing = false;
        enum GetClipboardImageStartFail
        {
            None,
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
        /// </br>
        /// </br>
        /// </summary>  
        /// <returns>Возращает изображение из буффера обменна в формате  <see cref="BitmapSource"/>.</returns>
        protected internal override async Task<T> Start<T, T2>(T2 parameter, CancellationToken? token = null)
        {
            CancellationToken Cancel = token ??= default;


            var StartFunctionFails = ExHelp.GetLazzyDictionaryFails
            (
              new KeyValuePair<GetClipboardImageStartFail, string>(GetClipboardImageStartFail.It_Is_Not_Possible_To_Perform_a_Startup_Before_The_Current_Operation_is_Complete, "Невозможно выполнить запуск до завершения текущей операции"),//0
              new KeyValuePair<GetClipboardImageStartFail, string>(GetClipboardImageStartFail.Clipboard_Empty, "В буффере обмена остувует изображение"),                                                                                      //1
              new KeyValuePair<GetClipboardImageStartFail, string>(GetClipboardImageStartFail.The_Output_Type_Is_Not_BitmapSource, $"Тип {nameof(T)} долженя вляется {typeof(BitmapSource)}"),                                                //2
              new KeyValuePair<GetClipboardImageStartFail, string>(GetClipboardImageStartFail.OperationCanceled, $"Операция операция получила запрос отмены через {nameof(CancellationToken)} и была отмененна")                              //3
            );
            try
            {
                if (Equals(typeof(T), typeof(BitmapSource)) is not true) throw new InvalidOperationException().Report(StartFunctionFails.Value[2]);
                if (isProcessing is true) throw new InvalidOperationException().Report(StartFunctionFails.Value[0]);

                if (Cancel.IsCancellationRequested is true) throw new OperationCanceledException(Cancel).Report(StartFunctionFails.Value[3]);

                isProcessing = true;
                try
                {
                    if (await GetClipboardImage(Cancel) is not T source) throw new NullReferenceException().Report(StartFunctionFails.Value[1]);
                    return source;
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
        private async Task<BitmapSource?> GetClipboardImage(CancellationToken? token = null)
        {
            var GetClipboardImageFails = ExHelp.GetLazzyDictionaryFails
            (
              new KeyValuePair<GetClipboardImageFail, string>(GetClipboardImageFail.OperationCanceled, $"Операция была отменена через {nameof(token)}") //0           
            );

            CancellationToken Cancel = token ??= default;

            if (Cancel.IsCancellationRequested is true) throw new OperationCanceledException(Cancel).Report(GetClipboardImageFails.Value[0]);

            BitmapSource? ReturnValue = null;
            Thread STAThread = new Thread(() =>
            {
                if (Clipboard.ContainsImage() is true)
                {
                    BitmapSource res = Clipboard.GetImage();
                    RenderOptions.SetBitmapScalingMode(res, BitmapScalingMode.NearestNeighbor);
                    res.Freeze();
                    ReturnValue = res;
                }

            });
            if (Cancel.IsCancellationRequested is true) throw new OperationCanceledException(Cancel).Report(GetClipboardImageFails.Value[0]);

            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return ReturnValue;
        }
        #endregion                   

    }
}
