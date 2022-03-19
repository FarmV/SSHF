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

        protected internal virtual bool IsCheceked 
        {
            get; 
            private set;
        }
        protected internal virtual string? Name { get;}

        /// <summary>
        /// Это базовый метод
        /// </summary>
        /// <returns></returns>
        protected internal abstract Task<bool> PreChecStart<T>(T parameter); 

        protected internal abstract Task<T> Start<T,T2>(T2 parameter);

        enum CacnelFail
        {
            None,
            FuctionNotImplementation
        }
        protected internal virtual Task<bool> Cancel()
        {
            throw new NotImplementedException().Report(new DictionaryEntry(CacnelFail.FuctionNotImplementation,"Реализация по умолчанию для функции не предусмотренна"));
        }

        internal static Lazy<DictionaryEntry[]> GetLazzyDictionaryFails(params DictionaryEntry[] dictionary) => new Lazy<DictionaryEntry[]>(new Func<DictionaryEntry[]>(() => dictionary));
    }
    internal static class ExHelp
    {
        internal static Exception Report(this Exception ex, DictionaryEntry report)
        {
            ex.Data[report.Key] = report.Value;
            return ex;
        }
        internal static Task<bool> ExCheck<T>(T res, Exception ex) where T : Enum
        {
            foreach (DictionaryEntry Dictionary in ex.Data)
            {
                if (Equals((Enum)Dictionary.Key, (Enum)res)) return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }      
    }
}



