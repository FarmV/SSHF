using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal abstract class BaseAlgorithm
    {
          
        protected internal virtual event EventHandler<EventArgs>? Complite;

        protected internal abstract bool IsCheceked { get; }
        protected internal abstract string Name { get; }

        /// <summary>
        /// Это базовый метод
        /// </summary>
        /// <returns></returns>
        protected internal abstract Task<bool> PreCheckStart<T>(T parameter); 

        protected internal abstract Task<T> Start<T,T2>(T2 parameter);

        enum CacnelFail
        {
            None,
            FuctionNotImplementation
        }
        protected internal virtual Task<bool> Cancel() =>
        throw new NotImplementedException().Report(new KeyValuePair<CacnelFail,string>(CacnelFail.FuctionNotImplementation,"Реализация по умолчанию для функции не предусмотренна"));
             
        internal static Lazy<KeyValuePair<T1, T2>[]> GetLazzyDictionaryFails<T1, T2>(params KeyValuePair<T1, T2>[] dictionary) where T1 : Enum
            => new Lazy<KeyValuePair<T1, T2>[]>(new Func<KeyValuePair<T1, T2>[]>(() => dictionary ));
    }
    internal static class ExHelp
    {
        internal static Exception Report<T,T2>(this Exception ex, KeyValuePair<T, T2> report) where T: Enum
        {
            ex.Data[report.Key] = report.Value;
            return ex;
        }
        internal static Task<bool> ExCheck<T,T2>(T res, Exception ex) where T : Enum
        {
            foreach (KeyValuePair<T,T2> Dictionary in ex.Data)
            {
                if (Equals(Dictionary.Key, res)) return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }      
    }
}



