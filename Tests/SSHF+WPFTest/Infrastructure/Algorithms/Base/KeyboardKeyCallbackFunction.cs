using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SSHF.Infrastructure.Algorithms.Base;
using System.Collections.Specialized;
using SSHF.Infrastructure.Algorithms.Input;
using SSHF.Infrastructure.Algorithms.KeyBoards.Base;
using SSHF.Infrastructure.Algorithms.Input.Keybord.Base;
using System.Collections.ObjectModel;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal class KeyboardKeyCallbackFunction
    {
        private static class Tasks<T> where T : Task
        {
            internal static Dictionary<VKeys[], T> FunctionsCallback = new Dictionary<VKeys[], T>();
        }
        public KeyboardKeyCallbackFunction()
        {
            KeyBordBaseRawInput.ChangeTheKeyPressure += CheckingForAMatchAndCallingAFunction;
        }

        enum CheckingForAMatchAndCallingAFunctionFail
        {
            None,
            CollectionIsNull,
            NewItemsIsNotVKeys
        }
        private void CheckingForAMatchAndCallingAFunction(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var CheckingForAMatchAndCallingAFunctionFails = BaseAlgorithm.GetLazzyDictionaryFails
                (new KeyValuePair<CheckingForAMatchAndCallingAFunctionFail, string>
                  (CheckingForAMatchAndCallingAFunctionFail.CollectionIsNull, $"{nameof(e.NewItems)} is Null"), //0
                  new KeyValuePair<CheckingForAMatchAndCallingAFunctionFail,string>
                  (CheckingForAMatchAndCallingAFunctionFail.NewItemsIsNotVKeys,$"{nameof(e.NewItems)} не являются {nameof(ObservableCollection<VKeys>)}") //1
                );

            if(e.NewItems is null) throw new NullReferenceException().Report(CheckingForAMatchAndCallingAFunctionFails.Value[0]);
            if (e.NewItems.Count is 0 ) return;
            if(e.NewItems is not ObservableCollection<VKeys> items) throw new  InvalidOperationException().Report(CheckingForAMatchAndCallingAFunctionFails.Value[1]);

            VKeys[] keys = new VKeys[items.Count];

            for (int i = 0;i < keys.Length;i++)
            {
                keys[i] = items[i];
            }
            try
            {
               Tasks<Task>.FunctionsCallback[keys].Start();
            }
            catch (Exception)
            {
                throw;
            }
        }
       internal void AddCallBackTask<T>(VKeys[] keyCombo, T callback) where T : Task
       {
            Tasks<T>.FunctionsCallback.Add(keyCombo, callback);
       }
    }
    
}
