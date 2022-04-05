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
using System.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal class VKeysEqualityComparer: IEqualityComparer<VKeys[]>
    {
        public bool Equals(VKeys[]? x, VKeys[]? y)
        {
            if (x is null || y is null) return false;

            if (x.Length is 0 || y.Length is 0) return false;

            return x.SequenceEqual(y);

        }

        public int GetHashCode([DisallowNull] VKeys[] obj)
        {
            return 0;
        }
    }
    internal class KeyboardKeyCallbackFunction
    {

        private readonly static KeyboardKeyCallbackFunction Instance = new KeyboardKeyCallbackFunction();
        private KeyboardKeyCallbackFunction()
        {

            KeyBordBaseRawInput input = KeyBordBaseRawInput.GetInstance();
            KeyBordBaseRawInput.ChangeTheKeyPressure += KeyBordBaseRawInput_ChangeTheKeyPressure;
        }

        private async static void KeyBordBaseRawInput_ChangeTheKeyPressure(object? sender, DataKeysNotificator e)
        {

            VKeys[] pressedKeys = e.Keys;
            if (pressedKeys.Length is 0) return;
            
            VKeys[][]? keys =
            await Task.Run(() =>
            {
                return keys = Tasks.FunctionsCallback.Keys.Where(x =>
                {
                    if (pressedKeys.Length != x.Length) return false;

                    for (int i = 0;i < pressedKeys.Length;i++)
                    {
                        if (x.Any((x) => x == pressedKeys[i]) is not true) return false;
                    }
                    return true;
                }).ToArray();
            });
            if (keys.Length is 0) return;
            if (keys.Length > 1) throw new InvalidOperationException();
            _ = Task.Run(() =>
            {
                try
                {
                    Tasks.FunctionsCallback[keys.ToArray()[0]].Invoke().Start();
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception)
                {

                }
            }).ConfigureAwait(false);
        }

        internal static KeyboardKeyCallbackFunction GetInstance() => Instance;

        private static class Tasks
        {
            internal static Dictionary<VKeys[], Func<Task>> FunctionsCallback = new Dictionary<VKeys[], Func<Task>>(new VKeysEqualityComparer());
        }

        enum AddCallBackTaskFail
        {
            None,
            KeysAreAlreadyRegistered,
            IsOneKey,
            KeyCombinationEmpty
        }

        internal Task AddCallBackTask(VKeys[] keyCombo, Func<Task> callbackTask, bool isOneKey = default)
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

        internal static Task<IEnumerable<KeyValuePair<VKeys[], Func<Task>>>> ReturnCollectionRegistrationFunction() => new Task<IEnumerable<KeyValuePair<VKeys[], Func<Task>>>>(() =>
        {
            static IEnumerable<KeyValuePair<VKeys[], Func<Task>>> GetFunction()
            {
                foreach (var item in Tasks.FunctionsCallback)
                {
                    yield return item;
                }
            }
            return GetFunction();
        });

        internal static Task<bool> ContainsKeyComibantion(VKeys[] keyCombo) => new Task<bool>(()
            =>
        {
            if (Tasks.FunctionsCallback.ContainsKey(keyCombo) is true) return true; else return false;
        });



    }

}
