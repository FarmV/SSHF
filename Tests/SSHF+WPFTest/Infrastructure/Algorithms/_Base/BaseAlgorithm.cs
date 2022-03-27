using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;
namespace SSHF.Infrastructure.Algorithms.Base
{
    internal abstract class BaseAlgorithm
    {
        internal readonly string DirectoryAlgorithms = "Extension"; 

        protected internal virtual event EventHandler<EventArgs>? Complite;

        protected internal virtual bool IsCheceked { get => true; }
        protected internal abstract string Name { get; }

        /// <summary>
        /// Это базовый метод
        /// </summary>
        /// <returns></returns>
        protected internal virtual Task<bool> IsCheck<T>(T parameter) => Task.FromResult(true);

        /// <summary>
        /// Это базовый метод
        /// </summary>
        /// <returns></returns>
        protected internal abstract Task<T> Start<T, T2>(T2 parameter, CancellationToken? token = null);

        internal static Task RegisredKeys(VKeys[] keyCombo, Task task)
        {
            try
            {
                KeyboardKeyCallbackFunction callback = KeyboardKeyCallbackFunction.GetInstance();

                callback.AddCallBackTask(keyCombo, task);
                return Task.CompletedTask;
            }
            catch (Exception)
            {
              throw;
            }
        }

        enum CacnelFail
        {
            None,
            FuctionNotImplementation
        }
        protected internal virtual Task<bool> Cancel() =>
        throw new NotImplementedException().Report(new KeyValuePair<CacnelFail,string>(CacnelFail.FuctionNotImplementation,"Реализация по умолчанию для функции не предусмотренна"));
             
        

        
    }
    internal static class ExHelp
    {
        internal static Lazy<KeyValuePair<T1, T2>[]> GetLazzyDictionaryFails<T1, T2>(params KeyValuePair<T1, T2>[] dictionary) where T1 : Enum
            => new Lazy<KeyValuePair<T1, T2>[]>(() => dictionary);
        internal static Exception Report<T,T2>(this Exception ex, KeyValuePair<T, T2> report) where T: Enum
        {
            ex.Data[report.Key] = report.Value;
            return ex;
        }

        internal enum HelpOperationForNotification
        {
            None,
            Canecled,
            InvalidDirectory,
            ThePreviousOperationWasNotCompleted,
            CompilationFail

        }


        enum HelerReasonFailFail
        {
            None,
            InconsistencyEnum,
            
        }
        internal static string HelerReasonFail(HelpOperationForNotification operation, Type? type = null)
        {

           
            if (Enum.IsDefined(typeof(HelpOperationForNotification), operation) is not true) throw new InvalidCastException().
                    Report(new KeyValuePair<HelerReasonFailFail,string>(HelerReasonFailFail.InconsistencyEnum,$"В {nameof(HelerReasonFailFail)} не присутвует ключ"));


            if(operation is HelpOperationForNotification.Canecled & type is null) return $"Операция получила {typeof(CancellationToken)} и была оменена";
            if (operation is HelpOperationForNotification.Canecled & type is not null) return $"Операция полчуила {type?.Name} и была оменена";

            if (operation is HelpOperationForNotification.InvalidDirectory & type is null) return $"Неверная директория";
            if (operation is HelpOperationForNotification.InvalidDirectory & type is not null) return $"Неверная директория {nameof(type)}";

            if (operation is HelpOperationForNotification.ThePreviousOperationWasNotCompleted & type is null) return $"Предыдущий вызов операции не был завершён";
            if (operation is HelpOperationForNotification.ThePreviousOperationWasNotCompleted & type is not null) return $"Предыдущий вызов операции {type?.Name} не был завершён";

            if (operation is HelpOperationForNotification.CompilationFail & type is null) return $"Неудачяная компиляция";
            if (operation is HelpOperationForNotification.CompilationFail & type is not null) return $"Неудачяная компиляция {type?.Name}";


            throw new Exception($"Нельзя отправлять {nameof(HelpOperationForNotification.None)}");
        }
    }
}



