using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;


using FVH.Background.Input.Infrastructure;
using FVH.Background.Input.Infrastructure.Interfaces;

namespace FVH.Background.Input
{
    internal partial class CallbackFunctionKeyboard : IDisposable
    {
        private bool _isDispose = false;
        private readonly List<GroupFunctions> GlobalList = new List<GroupFunctions>();
        private readonly object _lockObject = new object();
        private readonly LowLevelKeyboard _lowLevelHook;
        private readonly Dispatcher _toCallbackDispatcher;
        private readonly HashSet<VKeys> _currentPressLogicKeys = new HashSet<VKeys>();
        internal event EventHandler<KeyboardEventArgs>? NotifyKeyboardEvent;
        public CallbackFunctionKeyboard(Dispatcher toCallbackDispatcher)
        {
            _toCallbackDispatcher = toCallbackDispatcher;

            _lowLevelHook = new LowLevelKeyboard();

            _lowLevelHook.KeyboardEventHandler += LowLevelHookKeyboardEventHandler;
        }
        internal void InstallHook() => _lowLevelHook.InstallHook();
        internal void UninstallHook()
        {
            _lowLevelHook.UninstallHook();
            _currentPressLogicKeys.Clear();
        }
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = true;
            _lowLevelHook.Dispose();
        }
        public List<GroupFunctions> ReturnGroupRegFunctions() => GlobalList.ToList();
        public Task AddCallbackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null)
        {
            lock(_lockObject)
            {
                GroupFunctions? queryContainGroup = GlobalList.SingleOrDefault(x => x.Combination.SequenceEqual(keyCombo));
                if(queryContainGroup is not null) queryContainGroup.Functions.Add(new Function(callbackTask, identifier));
                else
                {
                    GroupFunctions newGroupF = new GroupFunctions(keyCombo, new List<Function>());
                    newGroupF.Functions.Add(new Function(callbackTask, identifier));
                    GlobalList.Add(newGroupF);
                }
                return Task.CompletedTask;
            }
        }
        public Task<bool> DeleteTaskByAnIdentifier(object identifier)
        {
            lock(_lockObject)
            {
                if(identifier is null) return Task.FromResult(false);
                Function? queryF = null;
                GroupFunctions? queryResult = GlobalList.SingleOrDefault(x =>
                {
                    queryF = x.Functions.SingleOrDefault(x => x.Identifier is not null && x.Identifier.Equals(identifier));
                    return queryF is not null;
                });
                if(queryResult is null) return Task.FromResult(false);
                else
                {
                    if(queryResult.Functions.Remove(queryF ?? throw new NullReferenceException(nameof(queryF))) is not true) throw new InvalidOperationException();

                    GlobalList.Where(x => x.Functions.Any() is not true).ToList().ForEach(x => GlobalList.Remove(x));
                    return Task.FromResult(true);
                }
            }
        }
        public Task<bool> DeleteInvokeListByKeyCombination(VKeys[] keyCombo)
        {
            lock(_lockObject)
            {
                if(keyCombo.Length is 0) return Task.FromResult(false);
                GroupFunctions? queryResult = GlobalList.SingleOrDefault(x => x.Combination == keyCombo);
                if(queryResult is null) return Task.FromResult(false);
                if(GlobalList.Remove(queryResult) is not true) throw new InvalidOperationException();
                return Task.FromResult(true);
            }
        }
        public Task<bool> ContainsKeyCombination(VKeys[] keyCombo) => Task.FromResult(GlobalList.SingleOrDefault(x => x.Combination == keyCombo) is not null);

        private VKeys[] _activeCombination = Array.Empty<VKeys>();
        private bool _isCombinationActive = false;
        private void LowLevelHookKeyboardEventHandler(object? _, KeyboardEventArgs e)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool InvokeAndBreakIfStrongCommination()
            {
                VKeys[] fullKeyCombination = _currentPressLogicKeys.ToArray();

                IEnumerable<GroupFunctions> queryStrongLength = GlobalList.Where(x => x.Combination.Length == fullKeyCombination.Length);
                
                bool anyFunctionInvoked = false;
                if(queryStrongLength.Any())
                {
                    foreach(GroupFunctions item in queryStrongLength)
                    {
                        bool isForceStrongCombination = item.Combination.Except(fullKeyCombination).Any() is false;

                        if(isForceStrongCombination)
                        {
                            InvokeFunctions(item.Functions);
                            anyFunctionInvoked = true;
                            _activeCombination = item.Combination;
                        }
                    }
                }

                if(anyFunctionInvoked is true)
                {
                    _isCombinationActive = true;
                    e.BreakLogicKey = true;
                }

                return anyFunctionInvoked;
            }
            if(e.Type == KeyboardEventArgs.TypePhysicallyEvent.Up) 
            {
                _ = _currentPressLogicKeys.Remove(e.Key);

                if(_isCombinationActive && _activeCombination.Contains(e.Key))
                {
                    e.BreakLogicKey = true;
                    if(_currentPressLogicKeys.Count == 0)
                    {
                        _isCombinationActive = false; 
                        _activeCombination = Array.Empty<VKeys>(); // Сбрасываем активную комбинацию
                    }                    
                }

                NotifyKeyboardEvent?.Invoke(this, e);
            }
            if(e.Type == KeyboardEventArgs.TypePhysicallyEvent.Down)
            {
                _ = _currentPressLogicKeys.Add(e.Key);

                _ = InvokeAndBreakIfStrongCommination();

                NotifyKeyboardEvent?.Invoke(this, e);
            }
        }
        private void InvokeFunctions(IEnumerable<Function> toTaskInvoke)
        {
            if(toTaskInvoke.Any() is false) throw new InvalidOperationException("The collection cannot be empty");

            static async Task StartOrRunTask(Func<Task> taskFunc)
            {
                Task task = taskFunc.Invoke();
                if(task.Status == TaskStatus.Created) task.Start();
                await task;
            }
            _ = _toCallbackDispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await Task.WhenAll(toTaskInvoke.Select(x => StartOrRunTask(x.Callback)));
                }
                catch(Exception)
                {
                    throw;
                }
            }, DispatcherPriority.Send).Task.Unwrap();
        }
        internal partial class LowLevelKeyboard : CriticalFinalizerObject, IDisposable
        {
            private const int WH_KEYBOARD_LL = 13;
            private const int HC_ACTION = 0;
            private const uint _THREAD_ID_ALL_IN_CURRENT_DESKTOP = 0;
            private nint _hookID = nint.Zero;
            private bool _isDispose = false;
            private delegate nint KeyboardHookHandler(int nCode, WMEvent wParam, nint lParam);
            private KeyboardHookHandler? _lowLevelKeyboardHandler;
            private readonly HashSet<VKeys> KeyDownPhysicallyProcessed = new HashSet<VKeys>();
            internal event EventHandler<KeyboardEventArgs>? KeyboardEventHandler;
            internal LowLevelKeyboard() { }
            ~LowLevelKeyboard() => Dispose();
            public void Dispose()
            {
                if(_isDispose is true) return;
                UninstallHook();
                _isDispose = true;
                GC.SuppressFinalize(this);
            }
            internal void InstallHook()
            {
                ObjectDisposedException.ThrowIf(_isDispose, this);
                ArgumentNullException.ThrowIfNull(KeyboardEventHandler, nameof(KeyboardEventHandler));

                if(Process.GetCurrentProcess().MainModule is not ProcessModule module) throw new NullReferenceException(nameof(module));
                nint hMod = GetModuleHandleW(module.ModuleName);
                if(hMod == nint.Zero) throw new NullReferenceException(nameof(hMod));

                _lowLevelKeyboardHandler ??= new KeyboardHookHandler(LowLevelKeyboardProc);

                nint handleHookProcedure = SetWindowsHookExW(WH_KEYBOARD_LL, _lowLevelKeyboardHandler, hMod, _THREAD_ID_ALL_IN_CURRENT_DESKTOP);
                if(handleHookProcedure == nint.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{nameof(handleHookProcedure)}{Marshal.GetLastPInvokeErrorMessage()}");

                _hookID = handleHookProcedure;
            }
            internal void UninstallHook()
            {
                if(_hookID == nint.Zero) return;
                _ = UnhookWindowsHookEx(_hookID);
                _hookID = nint.Zero;
                KeyDownPhysicallyProcessed.Clear();
            }
            private nint LowLevelKeyboardProc(int nCode, WMEvent wParam, nint lParam)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                nint KeyDown()
                {
                    TagKBDLLHOOKSTRUCT keyboardStruct = Marshal.PtrToStructure<TagKBDLLHOOKSTRUCT>(lParam);

                    bool isRepeatDownLogicKey = KeyDownPhysicallyProcessed.Contains(keyboardStruct.VkCode);

                    if(isRepeatDownLogicKey is true) return CallNextHookEx(_hookID, nCode, wParam, lParam);

                    KeyboardEventArgs argDown = new KeyboardEventArgs(
                     keyboardStruct.VkCode,
                      KeyboardEventArgs.TypePhysicallyEvent.Down,
                       false);

                    KeyboardEventHandler!.Invoke(this, argDown);
                    if(argDown.BreakLogicKey is true) return (nint)1;
                    _ = KeyDownPhysicallyProcessed.Add(keyboardStruct.VkCode);
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                nint KeyUP()
                {
                    TagKBDLLHOOKSTRUCT keyboardStruct = Marshal.PtrToStructure<TagKBDLLHOOKSTRUCT>(lParam);

                    KeyboardEventArgs argUp = new KeyboardEventArgs(
                     keyboardStruct.VkCode,
                      KeyboardEventArgs.TypePhysicallyEvent.Up,
                       false);

                    KeyboardEventHandler!.Invoke(this, argUp);

                    if(argUp.BreakLogicKey is true)
                    {
                        _ = KeyDownPhysicallyProcessed.Remove(keyboardStruct.VkCode);
                        return (nint)1;
                    }

                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                if(nCode is HC_ACTION)
                {
                    switch(wParam)
                    {
                        case WMEvent.WM_KEYDOWN: return KeyDown();
                        case WMEvent.WM_SYSKEYDOWN: return KeyDown();
                        case WMEvent.WM_KEYUP: return KeyUP();
                        case WMEvent.WM_SYSKEYUP: return KeyUP();
                    }
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            private enum WMEvent : uint
            {
                WM_KEYDOWN = 256,
                WM_SYSKEYDOWN = 260,
                WM_KEYUP = 257,
                WM_SYSKEYUP = 261
            }
            /// <summary>
            /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private readonly struct TagKBDLLHOOKSTRUCT
            {
                internal readonly VKeys VkCode;
                internal readonly uint ScanCode;
                internal readonly uint Flags;
                internal readonly uint Time;  //GetMessageTime() для сообщениями. До переполнение примерно 49,71 дней.
                internal readonly nuint DwExtraInfo; // ?
            }
            [LibraryImport("user32", SetLastError = true)]
            private static partial nint SetWindowsHookExW(int idHook, KeyboardHookHandler lpfn, nint hMod, uint dwThreadId);
            [LibraryImport("user32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static partial bool UnhookWindowsHookEx(nint hhk);
            [LibraryImport("user32")]
            private static partial nint CallNextHookEx(nint hhk, int nCode, WMEvent wParam, nint lParam);
            [LibraryImport("Kernel32")]
            private static partial nint GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        }
    }
    internal class KeyboardEventArgs
    {
        internal enum TypePhysicallyEvent
        {
            Down = 1,
            Up = 2,
        }
        internal KeyboardEventArgs(VKeys key, TypePhysicallyEvent typeEvent, bool breakKey)
        {
            Key = key;
            Type = typeEvent;
            BreakLogicKey = breakKey;
        }
        internal VKeys Key { get; init; }
        internal TypePhysicallyEvent Type { get; set; }
        internal bool BreakLogicKey { get; set; } = false;
    }
}
