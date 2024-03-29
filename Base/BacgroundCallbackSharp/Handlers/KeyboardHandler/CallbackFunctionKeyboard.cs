﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;


using FVH.Background.Input.Infrastructure;
using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal class CallbackFunctionKeyboard : IKeyboardCallback, IInvoke
    {
        private readonly IKeyboardHandler _keyboardHandler;
        private readonly List<RegFunctionGroupKeyboard> GlobalList = new List<RegFunctionGroupKeyboard>();
        private readonly LowLevelKeyHook _lowLevelHook;
        private readonly Dictionary<VKeys[], Func<Task>> FunctionsCallback = new Dictionary<VKeys[], Func<Task>>(new VKeysEqualityComparer());
        private readonly object _lockObject = new object();
        private RegFunctionGroupKeyboard[] _breakKey;

        public CallbackFunctionKeyboard(IKeyboardHandler keyboardHandler, LowLevelKeyHook lowLevelHook)
        {
            _keyboardHandler = keyboardHandler;
            _lowLevelHook = lowLevelHook;
            _keyboardHandler.KeyPressEvent += RawKeyDownEvent;

            _breakKey = GlobalList.Where(x => x.KeyCombination.Length is 1).ToArray();

            _lowLevelHook.KeyDownEvent += LowLevelKeyDownEvent;
        }
        private void CalculateSingleKeys() => _breakKey = GlobalList.Where(x => x.KeyCombination.Length is 1).ToArray();
        private void LowLevelKeyDownEvent(object? sender, LowLevelKeyHook.EventKeyLowLevelHook e)
        {
            if(_breakKey.Length == 0) return;

            VKeys currentKey = e.Key;

            foreach(RegFunctionGroupKeyboard item in _breakKey)
            {
                if(item.KeyCombination[0] == currentKey)
                {
                    e.Break = true;
                    _ = InvokeFunctions(item.ListOfRegisteredFunctions);
                    break;
                }
            }
        }
        private async void RawKeyDownEvent(object? sender, IKeysNotifier e)
        {
            VKeys[] pressedKeys = e.Keys;
            async Task<bool> InvokeOneKey(VKeys key)
            {
                RegFunctionGroupKeyboard? qR = GlobalList.SingleOrDefault(x => x.KeyCombination.Length == 1 & x.KeyCombination[0] == key);
                if(qR is null) return false;
                else
                {
                    await InvokeFunctions(qR.ListOfRegisteredFunctions);
                    return true;
                }
            }
            if(pressedKeys.Length is 0) return;
            if(pressedKeys.Length is 1)
            {
                await InvokeOneKey(e.Keys[0]);
                return;
            }
            static IEnumerable<VKeys> GetDifference(IEnumerable<VKeys> a, IEnumerable<VKeys> b)
            {
                List<VKeys> difCollection = new List<VKeys>(a);
                b.ToList().ForEach(x => difCollection.Remove(x));
                return difCollection;
            }
            IEnumerable<RegFunctionGroupKeyboard> queryPreviewNotDuplicate = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length + 1).Where(x => x.KeyCombination.Except(pressedKeys).Count() is 1);
            IEnumerable<RegFunctionGroupKeyboard> queryPreviewDuplicate = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length + 1);

            IEnumerable<RegFunctionGroupKeyboard>? queryStrong = GlobalList.Where(x => x.KeyCombination.Length == pressedKeys.Length);
            if(queryStrong is not null && queryStrong.Any())
            {
                foreach(RegFunctionGroupKeyboard item in queryStrong)
                {
                    bool isForceStrongCombination = item.KeyCombination.Except(pressedKeys).Any() is false;

                    if(isForceStrongCombination is true)
                    {
                        await InvokeFunctions(item.ListOfRegisteredFunctions);
                        return;
                    }
                }
            }
            List<VKeys> previewListExpectedKeys = new List<VKeys>();
            if(queryPreviewNotDuplicate.Any() is false)
            {
                if(queryPreviewDuplicate.Any() is false)
                    return;
                else
                {
                    foreach(RegFunctionGroupKeyboard x in queryPreviewDuplicate)
                    {
                        IEnumerable<VKeys> resultDifference = GetDifference(x.KeyCombination, pressedKeys);
                        if(resultDifference.Count() is 1)
                            previewListExpectedKeys.Add(resultDifference.ToArray()[0]);
                    }
                }
            }
            else if(queryPreviewNotDuplicate.Any() is true)
            {
                IEnumerable<VKeys> preKeysGroup = queryPreviewNotDuplicate.Select(x => x.KeyCombination.Except(pressedKeys)).ToArray().Select(x => x.ToArray()[0]);

                VKeys? preKeyInput = await this.PreKeys(preKeysGroup);

                if(preKeyInput.HasValue is false)
                    return;
                else
                {
                    RegFunctionGroupKeyboard invokeQuery = queryPreviewNotDuplicate.Single(x => x.KeyCombination.Intersect(new VKeys[] { preKeyInput.Value }).Count() is 1);
                    await InvokeFunctions(invokeQuery.ListOfRegisteredFunctions);
                }
            }
            if(previewListExpectedKeys.Count is 0)
                return;
            {
                VKeys? preKeyInput2 = await this.PreKeys(previewListExpectedKeys);

                if(preKeyInput2.HasValue is false)
                    return;
                else
                {
                    RegFunctionGroupKeyboard invokeQuery = queryPreviewDuplicate.Single(x => x.KeyCombination.Intersect(new VKeys[] { preKeyInput2.Value }).Count() is 1);
                    await InvokeFunctions(invokeQuery.ListOfRegisteredFunctions);
                }
            }
        }
        public async Task InvokeFunctions(IEnumerable<IRegFunction> toTaskInvoke)
        {
            if(toTaskInvoke.Any() is false) throw new InvalidOperationException("The collection cannot be empty");

            static async Task StartOrRunTask(Func<Task> taskFunc)
            {
                Task task = taskFunc.Invoke();
                if(task.Status == TaskStatus.Created)
                {
                    task.Start();
                    await task;
                }
            }

            System.Windows.Threading.Dispatcher? dispatcher = null;
            try
            {
                dispatcher = System.Windows.Application.Current.Dispatcher;
            }
            catch { dispatcher = null; }

            if(dispatcher is not null) await Task.WhenAll(toTaskInvoke.Select(x => x.CallbackTask).Select(func => dispatcher.InvokeAsync(() => StartOrRunTask(func)).Task));
            else
            {
                if(Dispatcher.FromThread(Thread.CurrentThread) is not Dispatcher inputHandlerDispatcher) throw new NullReferenceException(nameof(inputHandlerDispatcher));

                await Task.WhenAll(toTaskInvoke.Select(x => x.CallbackTask).Select(func => inputHandlerDispatcher.InvokeAsync(() => StartOrRunTask(func)).Task));
            }
        }
        private async Task<VKeys?> PreKeys(IEnumerable<VKeys> keys)
        {
            if(_lowLevelHook is null) throw new NullReferenceException(nameof(LowLevelKeyHook));

            VKeys? res = null;
            bool complete = false;

            void CheckKeyCallback(object? _, LowLevelKeyHook.EventKeyLowLevelHook e)
            {
                if(_lowLevelHook is null)
                    throw new NullReferenceException(nameof(LowLevelKeyHook));
                VKeys? checkKey = null;

                checkKey = e.Key switch
                {
                    VKeys.VK_LCONTROL or VKeys.VK_RCONTROL => (VKeys?)VKeys.VK_CONTROL,
                    VKeys.VK_LMENU or VKeys.VK_RMENU => (VKeys?)VKeys.VK_MENU,
                    VKeys.VK_LSHIFT or VKeys.VK_RSHIFT => (VKeys?)VKeys.VK_SHIFT,
                    _ => (VKeys?)e.Key,
                };
                if(checkKey.HasValue is false)
                    throw new InvalidOperationException();

                Debug.WriteLine($"{keys.First()} - {checkKey.Value}");
                if(keys.Contains(checkKey.Value))
                {
                    res = checkKey;
                    e.Break = true;
                    _lowLevelHook.KeyDownEvent -= CheckKeyCallback;
                    complete = true;
                }
                else
                {
                    _lowLevelHook.KeyDownEvent -= CheckKeyCallback;
                    complete = true;
                }
            }
            return await Task.Run<VKeys?>(() =>
            {
                _lowLevelHook.KeyDownEvent += CheckKeyCallback;
                if(System.Threading.SpinWait.SpinUntil(() => complete is true, TimeSpan.FromMilliseconds(950)) is not true)
                {
                    Debug.WriteLine($"Warning Timeout {CheckKeyCallback}");
                }
                return res;
            });
        }
        public Task AddCallBackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null)
        {
            lock(_lockObject)
            {
                RegFunctionGroupKeyboard? queryContainGroup = GlobalList.SingleOrDefault(x => x.KeyCombination.SequenceEqual(keyCombo));
                if(queryContainGroup is not null) queryContainGroup.ListOfRegisteredFunctions.Add(new RegFunction(callbackTask, identifier));
                else
                {
                    RegFunctionGroupKeyboard newGroupF = new RegFunctionGroupKeyboard(keyCombo, new List<IRegFunction>());
                    newGroupF.ListOfRegisteredFunctions.Add(new RegFunction(callbackTask, identifier));
                    GlobalList.Add(newGroupF);
                }
                CalculateSingleKeys();
                return Task.CompletedTask;
            }
        }
        public Task<bool> DeleteTaskByAnIdentifier(object identifier)
        {
            lock(_lockObject)
            {
                if(identifier is null) return Task.FromResult(false);
                IRegFunction? queryF = null;
                RegFunctionGroupKeyboard? queryResult = GlobalList.SingleOrDefault(x =>
                {
                    queryF = x.ListOfRegisteredFunctions.SingleOrDefault(x => x.Identifier is not null && x.Identifier.Equals(identifier));
                    return queryF is not null;
                });
                if(queryResult is null) return Task.FromResult(false);
                else
                {
                    if(queryResult.ListOfRegisteredFunctions.Remove(queryF ?? throw new NullReferenceException(nameof(queryF))) is not true) throw new InvalidOperationException();

                    GlobalList.Where(x => x.ListOfRegisteredFunctions.Any() is not true).ToList().ForEach(x => GlobalList.Remove(x));
                    CalculateSingleKeys();
                    return Task.FromResult(true);
                }
            }
        }
        public Task<bool> DeleteInvokeListByKeyCombination(VKeys[] keyCombo)
        {
            lock(_lockObject)
            {
                if(keyCombo.Length is 0) return Task.FromResult(false);
                RegFunctionGroupKeyboard? queryResult = GlobalList.SingleOrDefault(x => x.KeyCombination == keyCombo);
                if(queryResult is null) return Task.FromResult(false);
                if(GlobalList.Remove(queryResult) is not true) throw new InvalidOperationException();
                CalculateSingleKeys();
                return Task.FromResult(true);
            }
        }
        public List<RegFunctionGroupKeyboard> ReturnGroupRegFunctions() => GlobalList.ToList();
        public Task<bool> ContainsKeyCombination(VKeys[] keyCombo) => Task.FromResult(GlobalList.SingleOrDefault(x => x.KeyCombination == keyCombo) is not null);
    }
}
