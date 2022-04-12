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

using System.Threading;
using System.Windows.Interop;
using System.Windows;

namespace SSHF.Infrastructure.Algorithms.Base
{
    internal class VKeysEqualityComparer : IEqualityComparer<VKeys[]>, IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await Task.Run(async () =>
        {
            await KeyboardKeyCallbackFunction.Lowlevlhook.DisposeAsync();
            GC.SuppressFinalize(this);
        });

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

        internal static MyLowlevlhook? Lowlevlhook;
        //  private readonly EventWaitHandle WaitHandlePreKeys = new EventWaitHandle(false, EventResetMode.AutoReset);
        private static async Task<VKeys?> PreKeys(List<VKeys> keys)
        {
            //EventWaitHandle WaitHandlePreKeys = new EventWaitHandle(false, EventResetMode.AutoReset);

            //_ = Task.Run(() =>
            //{
            //    using Timer breakTimer = new Timer(new TimerCallback((arg) =>
            //    {
            //        try
            //        {
            //            WaitHandlePreKeys.Set();

            //        } catch (Exception) { };
            //    }), null, 5000, Timeout.Infinite);
            //});
            //VKeys? res = null;
            //await Task.Run(() =>
            //{
            //    void CheckKey(VKeys key)
            //    {
            //        if (keys.Contains(key)) res = key;
            //        {
            //          //  isBreak = true;
            //            try
            //            {
            //                WaitHandlePreKeys.Set();
            //            } catch (Exception) { };
            //        }

            //    }
            //    MyLowlevlhook lowlevlhook = new MyLowlevlhook();
            //    lowlevlhook.InstallHook();
            //    lowlevlhook.KeyDown += CheckKey;
            //    WaitHandlePreKeys.WaitOne();
            //    //Alowlevlhook.UninstallHook();
            //    lowlevlhook.KeyDown -= CheckKey;
            //    WaitHandlePreKeys.Dispose();
            //});
            //return res;


            VKeys? dd = null;

            return dd;
        }

        private async static void KeyBordBaseRawInput_ChangeTheKeyPressure(object? sender, DataKeysNotificator e)
        {

            VKeys[] pressedKeys = e.Keys;
            if (pressedKeys.Length is 0) return;


            List<VKeys> listPreKeys = new List<VKeys>();


            List<VKeys[]> keys = Tasks.FunctionsCallback.Keys.Where(x =>  // Почему то метод пропускается при активации формы в хуке
            {
                for (int i = 0; i < pressedKeys.Length; i++)
                {
                    if (x.Any((x) => x == pressedKeys[i]) is not true)
                    {
                        return false;
                    }
                    if (x.Any((x) => x == pressedKeys[i]) is true)
                    {
                        if (x.Length - pressedKeys.Length is 1 & x.Length - 1 == i + 1)
                        {
                            listPreKeys.Add(x[^1]);
                        }
                    }
                }
                return true;
            }).Where(x => x.Length == pressedKeys.Length).ToList();

            if (listPreKeys.Count > 0)
            {
                VKeys? callhook = await PreKeys(listPreKeys);
                if (callhook.HasValue is true)
                {
                    VKeys[] preseedKeys1 = pressedKeys.Append(callhook.Value).ToArray();

                    keys = await Task.Run(() => Tasks.FunctionsCallback.Keys.Where(x =>
                    {
                        if (preseedKeys1.Length != x.Length)
                        {
                            throw new InvalidOperationException();
                        }

                        for (int i = 0; i < preseedKeys1.Length; i++)
                        {
                            if (x.Any((x) => x == preseedKeys1[i]) is not true)
                            {
                                return false;
                            }
                        }
                        return true;
                    }).Where(x => x.Length == pressedKeys.Length).ToList());
                }
            }

            if (keys.Count is 0) return;
            if (keys.Count > 1) throw new InvalidOperationException();
            _ = Task.Run(() =>
            {
                try
                {
                    Tasks.FunctionsCallback[keys[0]].Invoke().Start();
                } catch (InvalidOperationException)
                {
                    throw;
                } catch (Exception)
                {

                }
            }).ConfigureAwait(false);
        }

        internal static KeyboardKeyCallbackFunction GetInstance()
        {
            App.WindowsIsOpen[App.GetMyMainWindow].Value.Invoke(() =>
            {
                Lowlevlhook = new MyLowlevlhook();
                Lowlevlhook.InstallHook();

            });
            return Instance;
        }

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

        internal static Task<bool> ContainsKeyComibantion(VKeys[] keyCombo) => new Task<bool>(() =>
        {
            if (Tasks.FunctionsCallback.ContainsKey(keyCombo) is true) return true; else return false;
        });



    }

}
