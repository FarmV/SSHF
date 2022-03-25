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

        private readonly static KeyboardKeyCallbackFunction Instance = new KeyboardKeyCallbackFunction();
        private KeyboardKeyCallbackFunction()
        {

            KeyBordBaseRawInput input = KeyBordBaseRawInput.GetInstance();
            KeyBordBaseRawInput.ChangeTheKeyPressure += CheckingForAMatchAndCallingAFunction;
        }

        internal static KeyboardKeyCallbackFunction GetInstance() => Instance;

        private static class Tasks
        {
            internal static Dictionary<VKeys[], Task> FunctionsCallback = new Dictionary<VKeys[], Task>();
        }
       

        enum CheckingForAMatchAndCallingAFunctionFail
        {
            None,
            CollectionIsNull,
            NewItemsIsNotVKeys
        }
        private static void CheckingForAMatchAndCallingAFunction(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var CheckingForAMatchAndCallingAFunctionFails = ExHelp.GetLazzyDictionaryFails
                (new KeyValuePair<CheckingForAMatchAndCallingAFunctionFail, string>
                  (CheckingForAMatchAndCallingAFunctionFail.CollectionIsNull, $"{nameof(e.NewItems)} is Null"),                                            //0
                  new KeyValuePair<CheckingForAMatchAndCallingAFunctionFail, string>
                  (CheckingForAMatchAndCallingAFunctionFail.NewItemsIsNotVKeys, $"{nameof(e.NewItems)} не являются {nameof(ObservableCollection<VKeys>)}") //1
                );

            if (e.NewItems is null) throw new NullReferenceException().Report(CheckingForAMatchAndCallingAFunctionFails.Value[0]);
            if (e.NewItems.Count is 0) return;
            if (e.NewItems is not ObservableCollection<VKeys> items) throw new InvalidOperationException().Report(CheckingForAMatchAndCallingAFunctionFails.Value[1]);

            VKeys[] keys = new VKeys[items.Count];

            for (int i = 0;i < keys.Length;i++)
            {
                keys[i] = items[i];
            }
            try
            {
                Tasks.FunctionsCallback[keys].Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        enum AddCallBackTaskFail
        {
            None,
            KeysAreAlreadyRegistered,
            IsOneKey,
            KeyCombinationEmpty
        }
        internal Task AddCallBackTask(VKeys[] keyCombo, Task callbackTask, bool isOneKey = default)
        {
            var AddCallBackTaskFails = ExHelp.GetLazzyDictionaryFails
                (
                 new KeyValuePair<AddCallBackTaskFail, string>(AddCallBackTaskFail.KeysAreAlreadyRegistered, "Комибинация клавиш(клавиши) уже зарегистрированна"), //0
                 new KeyValuePair<AddCallBackTaskFail, string>(AddCallBackTaskFail.IsOneKey, $"Была передана одна клавиша для регистрации с ключем {nameof(isOneKey)} false"), //1
                 new KeyValuePair<AddCallBackTaskFail, string>(AddCallBackTaskFail.KeyCombinationEmpty, $"Невозможно зарегистрировать пустую комбинацию клавиш") //2
                ); 

            if (keyCombo.Length is 0) throw new InvalidOperationException().Report(AddCallBackTaskFails.Value[2]);

            if (Tasks.FunctionsCallback.ContainsKey(keyCombo) is true) throw new InvalidOperationException().Report(AddCallBackTaskFails.Value[0]);

            if (keyCombo.Length is 1 & isOneKey is false) throw new InvalidOperationException().Report(AddCallBackTaskFails.Value[1]);


            Tasks.FunctionsCallback.Add(keyCombo, callbackTask);

            return Task.CompletedTask;
        }

        internal static Task<IEnumerable<KeyValuePair<VKeys[], Task>>> ReturnCollectionRegistrationFunction() => new Task<IEnumerable<KeyValuePair<VKeys[], Task>>>(()=>
        {
            static IEnumerable<KeyValuePair<VKeys[], Task>> GetFunction()
            {
                foreach(var item in Tasks.FunctionsCallback)
                {
                    yield return item;
                }
            } return GetFunction();
        });

        internal static Task<bool> ContainsKeyComibantion(VKeys[] keyCombo) => new Task<bool>(()
            => {if (Tasks.FunctionsCallback.ContainsKey(keyCombo) is true) return true; else return false; });



    }

}
