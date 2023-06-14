using FVH.Background.Input;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FVH.Background.Input
{
    internal class CallbackFunctionKeyboard : IKeyboardCallback, IInvoke
    {
        private readonly List<RegFunctionGroupKeyboard> GlobalList = new List<RegFunctionGroupKeyboard>();
        private readonly LowLevlHook _lowlevlhook;
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly Dictionary<VKeys[], Func<Task>> FunctionsCallback = new Dictionary<VKeys[], Func<Task>>(new VKeysEqualityComparer());

        public CallbackFunctionKeyboard(IKeyboardHandler keyboardHandler, LowLevlHook lowLevlHook)
        {
            _keyboardHandler = keyboardHandler;
            _lowlevlhook = lowLevlHook;
            _keyboardHandler.KeyPressEvent += KeyboardHandler_KeyPressEvent;
        }
        private async void KeyboardHandler_KeyPressEvent(object? sender, IKeysNotificator e)
        {
            VKeys[] pressedKeys = e.Keys;
            async Task<bool> InvokeOneKey(VKeys key)
            {
                RegFunctionGroupKeyboard? qR = GlobalList.SingleOrDefault(x => x.KeyCombination.Length == 1 & x.KeyCombination[0] == key);
                if (qR is null) return false;
                else
                {
                    await InvokeFunctions(qR.ListOfRegisteredFunctions);
                    return true;
                }
            }
            if (pressedKeys.Length is 0) return;
            if (pressedKeys.Length is 1)
            {
                await InvokeOneKey(e.Keys[0]);
                return;
            }
            static IEnumerable<VKeys> GetDifference(IEnumerable<VKeys> a, IEnumerable<VKeys> b)
            {
                List<VKeys> difColection = new List<VKeys>(a);
                b.ToList().ForEach(x => difColection.Remove(x));
                return difColection;
            }
            IEnumerable<RegFunctionGroupKeyboard> queryPrewievNotDuplicate = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length + 1).Where(x => x.KeyCombination.Except(pressedKeys).Count() == 1);
            IEnumerable<RegFunctionGroupKeyboard> queryPrewievDuplicate = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length + 1);

            IEnumerable<RegFunctionGroupKeyboard>? queryStrong = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length);
            if (queryStrong is not null && queryStrong.Any())
            {
                foreach (RegFunctionGroupKeyboard item in queryStrong)
                {

                    IEnumerable<VKeys> r0 = item.KeyCombination.Except(pressedKeys);//Заглушка?
                    bool r1 = item.KeyCombination.Except(pressedKeys).Any();

                    // bool r1 = item.KeyCombination.Except(pressedKeys).Any(); //todo плавущая ошибка null 
                    if (r1 is false)
                    {
                        await InvokeFunctions(item.ListOfRegisteredFunctions);
                        return;
                    }
                }
            }

            List<VKeys> myPreKeys = new List<VKeys>();
            if (queryPrewievNotDuplicate.Any() is false)
            {
                if (queryPrewievDuplicate.Any() is false) return;
                else
                {
                    foreach (RegFunctionGroupKeyboard x in queryPrewievDuplicate)
                    {
                        IEnumerable<VKeys> resultDifference = GetDifference(x.KeyCombination, pressedKeys);
                        if (resultDifference.Count() == 1) myPreKeys.Add(resultDifference.ToArray()[0]);
                    }
                }
            }
            else if (queryPrewievNotDuplicate.Any() is true)
            {
                IEnumerable<VKeys> preKeysGroup = queryPrewievNotDuplicate.Select(x => x.KeyCombination.Except(pressedKeys)).ToArray().Select(x => x.ToArray()[0]);

                VKeys? preKeyInput = await PreKeys(preKeysGroup);

                if (preKeyInput.HasValue is false) return;
                else
                {
                    RegFunctionGroupKeyboard invokeQuery = queryPrewievNotDuplicate.Single(x => x.KeyCombination.Intersect(new VKeys[] { preKeyInput.Value }).Count() == 1);
                    await InvokeFunctions(invokeQuery.ListOfRegisteredFunctions);
                }
            }
            if (myPreKeys.Count is 0) return;
            {
                VKeys? preKeyInput2 = await PreKeys(myPreKeys);

                if (preKeyInput2.HasValue is false) return;
                else
                {
                    RegFunctionGroupKeyboard invokeQuery = queryPrewievDuplicate.Single(x => x.KeyCombination.Intersect(new VKeys[] { preKeyInput2.Value }).Count() == 1);
                    await InvokeFunctions(invokeQuery.ListOfRegisteredFunctions);
                }
            }
        }

        public Task InvokeFunctions(IEnumerable<IRegFunction> toTaskInvoke) //todo порверить и возможно перерабоать логику обработки исключений
        {
            if (toTaskInvoke.Any() is false) throw new InvalidOperationException("The collection cannot be empty");
            Task.Run(() => Parallel.Invoke(toTaskInvoke.Select(x => new Action(async () =>
            {
                Task invokeTask = x.CallBackTask.Invoke();
                if (invokeTask.IsCompleted is true)
                {
                    try
                    {
                        await invokeTask.ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        //todo Сделать уведомление о ошбках?
                    }
                }
                else
                {
                    try
                    {
                        invokeTask.Start();
                    }
                    catch (Exception)
                    {
                        //todo Сделать уведомление о ошбках?
                    }
                }

            })).ToArray()));

            return Task.CompletedTask;
        }

        private async Task<VKeys?> PreKeys(IEnumerable<VKeys> keys)
        {
            if (_lowlevlhook is null) throw new NullReferenceException(nameof(LowLevlHook));
            VKeys? res = null;

            bool ret = default;

            _ = Task.Run(() =>
              {

                  _lowlevlhook.KeyDown += CheckKey;

                  void CheckKey(VKeys key, LowLevlHook.SettingHook setting)
                  {
                      if (_lowlevlhook is null) throw new NullReferenceException(nameof(LowLevlHook));
                      VKeys? chekKey = null;
                      if (key is VKeys.VK_LCONTROL || key is VKeys.VK_RCONTROL) // Заглушки из за разности между RawInput(не рапознает правый левый) и хуком, так как предположительно нужно менять исходную библиотеку.
                      {
                          chekKey = VKeys.VK_CONTROL;
                      }
                      else if (key is VKeys.VK_LMENU || key is VKeys.VK_RMENU)
                      {
                          chekKey = VKeys.VK_MENU;
                      }
                      else if (key is VKeys.VK_LSHIFT || key is VKeys.VK_RSHIFT)
                      {
                          chekKey = VKeys.VK_SHIFT;
                      }
                      else
                      {
                          chekKey = key;
                      }
                      if (chekKey.HasValue is false) throw new InvalidOperationException();
                      bool res1 = keys.Contains(chekKey.Value);

                      Debug.WriteLine($"{keys.First()} - {chekKey.Value}");
                      if (keys.Contains(chekKey.Value))
                      {
                          res = chekKey;
                          setting.Break = true;
                          _lowlevlhook.KeyDown -= CheckKey;
                          ret = true;
                      }
                      else
                      {
                          _lowlevlhook.KeyDown -= CheckKey;
                          ret = true;
                      }
                  }
              });

            for (int i = 0; i < 1000; i++)
            {
                if (ret is true) break;
                await Task.Delay(10);
            }
            return res;
        }


        private int GoupCount = 0;
        private readonly object _lockMedthod = new object();
        public Task AddCallBackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null)
        {
            lock (_lockMedthod)
            {
                RegFunctionGroupKeyboard? queryCotainGroup = GlobalList.SingleOrDefault(x => x.KeyCombination.SequenceEqual(keyCombo));
                if (queryCotainGroup is not null) queryCotainGroup.ListOfRegisteredFunctions.Add(new RegFunction(callbackTask, identifier));
                else
                {
                    RegFunctionGroupKeyboard newGroupF = new RegFunctionGroupKeyboard(++GoupCount, keyCombo, new List<IRegFunction>());
                    newGroupF.ListOfRegisteredFunctions.Add(new RegFunction(callbackTask, identifier));
                    GlobalList.Add(newGroupF);
                }

                return Task.CompletedTask;
            }
        }
        public Task<bool> DeleteATaskByAnIdentifier(object identifier)
        {
            lock (_lockMedthod)
            {
                if (identifier is null) return Task.FromResult(false);
                IRegFunction? queryF = null;
                RegFunctionGroupKeyboard? queryResult = GlobalList.SingleOrDefault(x =>
                {
                    queryF = x.ListOfRegisteredFunctions.SingleOrDefault(x => x.Identifier is not null && x.Identifier.Equals(identifier));
                    return queryF is not null;
                });
                if (queryResult is null) return Task.FromResult(false);
                else
                {
                    if (queryResult.ListOfRegisteredFunctions.Remove(queryF ?? throw new NullReferenceException(nameof(queryF))) is not true) throw new InvalidOperationException();
                    else
                    {
                        GlobalList.Where(x => x.ListOfRegisteredFunctions.Any() is not true).ToList().ForEach(x => GlobalList.Remove(x));
                        return Task.FromResult(true);
                    }
                }
            }
        }
        public Task<bool> DeleteAGroupByKeyСombination(VKeys[] keyCombo)
        {
            lock (_lockMedthod)
            {
                if (keyCombo.Length is 0) return Task.FromResult(false);
                RegFunctionGroupKeyboard? queyResult = GlobalList.SingleOrDefault(x => x.KeyCombination == keyCombo);
                if (queyResult is null) return Task.FromResult(false);
                if (GlobalList.Remove(queyResult) is not true) throw new InvalidOperationException();
                return Task.FromResult(true);
            }
        }

        public List<RegFunctionGroupKeyboard> ReturnGroupRegFunctions() => GlobalList.ToList();

        public Task<bool> ContainsKeyComibantion(VKeys[] keyCombo) => Task.FromResult(GlobalList.SingleOrDefault(x => x.KeyCombination == keyCombo) is not null);
    }

}
