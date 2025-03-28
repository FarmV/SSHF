using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
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
        private VKeys[] _activeCombination;
        private bool _isCombinationActive = false;
        private readonly Lock _lockObject = new Lock();
        private readonly List<GroupFunctions> _globalCallbackList;
        private readonly LowLevelKeyboard _lowLevelHook;
        private readonly Dispatcher _toCallbackDispatcher;
        private readonly HashSet<VKeys> _currentPressLogicKeys;
        internal event LowLevelKeyboard.KeyboardEventHandler? NotifyKeyboardEvent;
        public CallbackFunctionKeyboard(Dispatcher toCallbackDispatcher)
        {
            _activeCombination = Array.Empty<VKeys>();
            _globalCallbackList = new List<GroupFunctions>();
            _currentPressLogicKeys = new HashSet<VKeys>();

            _toCallbackDispatcher = toCallbackDispatcher;
            _lowLevelHook = new LowLevelKeyboard();
            _lowLevelHook.KeyAction += LowLevelHookKeyboardEventHandler;
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
        public List<GroupFunctions> ReturnGroupRegFunctions() => _globalCallbackList.ToList();
        public Task AddCallbackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null, Func<bool>? canExecute = null)
        {
            lock(_lockObject)
            {
                GroupFunctions? queryContainGroup = _globalCallbackList.SingleOrDefault(x => x.Combination.SequenceEqual(keyCombo));
                if(queryContainGroup is not null) queryContainGroup.Functions.Add(new Function(callbackTask, identifier));
                else
                {
                    GroupFunctions newGroupF = new GroupFunctions(keyCombo, new List<Function>());
                    newGroupF.Functions.Add(new Function(callbackTask, identifier, canExecute));
                    _globalCallbackList.Add(newGroupF);
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
                GroupFunctions? queryResult = _globalCallbackList.SingleOrDefault(x =>
                {
                    queryF = x.Functions.SingleOrDefault(x => x.Identifier is not null && x.Identifier.Equals(identifier));
                    return queryF is not null;
                });
                if(queryResult is null) return Task.FromResult(false);
                else
                {
                    if(queryResult.Functions.Remove(queryF ?? throw new NullReferenceException(nameof(queryF))) is not true) throw new InvalidOperationException();

                    _globalCallbackList.Where(x => x.Functions.Any() is not true).ToList().ForEach(x => _globalCallbackList.Remove(x));
                    return Task.FromResult(true);
                }
            }
        }
        public Task<bool> DeleteInvokeListByKeyCombination(VKeys[] keyCombo)
        {
            lock(_lockObject)
            {
                if(keyCombo.Length is 0) return Task.FromResult(false);
                GroupFunctions? queryResult = _globalCallbackList.SingleOrDefault(x => x.Combination == keyCombo);
                if(queryResult is null) return Task.FromResult(false);
                if(_globalCallbackList.Remove(queryResult) is not true) throw new InvalidOperationException();
                return Task.FromResult(true);
            }
        }
        public Task<bool> ContainsKeyCombination(VKeys[] keyCombo) => Task.FromResult(_globalCallbackList.SingleOrDefault(x => x.Combination == keyCombo) is not null);
        private void LowLevelHookKeyboardEventHandler(ref KeyboardEventArgs e)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool InvokeAndBreakIfStrongCommination(ref KeyboardEventArgs ev)
            {
                bool anyInvoked = false;
 
                IEnumerable<GroupFunctions> queryStrongLength = _globalCallbackList.Where((GroupFunctions g) => g.Combination.Length == _currentPressLogicKeys.Count);
                
                if(queryStrongLength.Any())
                {
                    foreach(GroupFunctions item in queryStrongLength)
                    {
                        bool isForceStrongCombination = item.Combination.Except(_currentPressLogicKeys).Any() is false;

                        if(isForceStrongCombination)
                        {
                            anyInvoked = AnyInvokeFunctions(item.Functions);                
                            _activeCombination = item.Combination;
                        }
                    }
                }

                if(anyInvoked is false)
                {
                    _isCombinationActive = false;
                    _activeCombination = Array.Empty<VKeys>();
                    return anyInvoked;
                }
                else
                {
                    _isCombinationActive = true;
                    ev.BreakLogicKey = true;
                }

                return anyInvoked;
            }
            if(e.Type == KeyboardEventArgs.TypePhysicallyEvent.ForceClearState)
            {
                _currentPressLogicKeys.Clear();
                _activeCombination = Array.Empty<VKeys>();
                _isCombinationActive = false;
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
                        _activeCombination = Array.Empty<VKeys>(); 
                    }                    
                }

                NotifyKeyboardEvent?.Invoke(ref e);
                return;
            }
            if(e.Type == KeyboardEventArgs.TypePhysicallyEvent.Down)
            {
                _ = _currentPressLogicKeys.Add(e.Key);

                if(e.IsDownRepeat is true && _isCombinationActive is true)
                {
                    e.BreakLogicKey = true;
                    NotifyKeyboardEvent?.Invoke(ref e);
                    return;
                }

                _ = InvokeAndBreakIfStrongCommination(ref e);
         
                NotifyKeyboardEvent?.Invoke(ref e);
            }
        }
        private bool AnyInvokeFunctions(IEnumerable<Function> toTaskInvoke)
        {
            static async Task StartOrRunTask(Func<Task> taskFunc)
            {
                Task task = taskFunc.Invoke();
                if(task.Status == TaskStatus.Created) task.Start();
                await task;
            }

            bool isAny = false;

            if(toTaskInvoke.Any() is false) throw new InvalidOperationException("The collection cannot be empty");

            IEnumerable<Function> toCanExecute = toTaskInvoke.Where(static (Function f) => f.CanExecute.Invoke() is true);

            isAny = toCanExecute.Any();
            if(isAny is false) return isAny;

            _ = _toCallbackDispatcher.InvokeAsync(async () => await Task.WhenAll(toCanExecute.Select(static (Function f) => StartOrRunTask(f.Callback))).ConfigureAwait(false),
             DispatcherPriority.Send).Task.Unwrap().ContinueWith((Task t) => 
              _ = ThreadPool.QueueUserWorkItem((object? __) => throw t.Exception ?? new AggregateException($"Callback task is faulted. Id task => {t.Id}")),TaskContinuationOptions.OnlyOnFaulted);
            return isAny;
        }
        internal partial class LowLevelKeyboard : CriticalFinalizerObject, IDisposable
        {
            private const int WH_KEYBOARD_LL = 13;
            private const int HC_ACTION = 0;
            const uint WINEVENT_OUTOFCONTEXT = 0x0000; 
            const uint EVENT_SYSTEM_DESKTOPSWITCH = 0x0020; 
            private const uint ThreadIdAllInCurrentDesktop = 0;
            private nint _hookID = nint.Zero;
            private nint _hDesktopSwitchHook = nint.Zero;
            private bool _isDispose = false;
            private delegate nint KeyboardHookHandler(int nCode, WMEvent wParam, nint lParam);
            private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
            private WinEventDelegate? _desktopSwitchDelegate;
            private KeyboardHookHandler? _lowLevelKeyboardHandler;
            private readonly HashSet<VKeys> KeyDownPhysicallyProcessed;
            internal delegate void KeyboardEventHandler(ref KeyboardEventArgs args);
            internal event KeyboardEventHandler? KeyAction;
            internal LowLevelKeyboard() => KeyDownPhysicallyProcessed = new HashSet<VKeys>();
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
                ArgumentNullException.ThrowIfNull(KeyAction, nameof(KeyboardEventHandler));

                if(_hookID == nint.Zero)
                {
                    if(Process.GetCurrentProcess().MainModule is not ProcessModule module) throw new NullReferenceException(nameof(module));
                    nint hMod = GetModuleHandleW(module.ModuleName);
                    if(hMod == nint.Zero) throw new NullReferenceException(nameof(hMod));

                    _lowLevelKeyboardHandler ??= new KeyboardHookHandler(LowLevelKeyboardProc);

                    nint handleHookProcedure = SetWindowsHookExW(WH_KEYBOARD_LL, _lowLevelKeyboardHandler, hMod, ThreadIdAllInCurrentDesktop);
                    if(handleHookProcedure == nint.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError(), $"{nameof(handleHookProcedure)}{Marshal.GetLastPInvokeErrorMessage()}");

                    _hookID = handleHookProcedure;
                }
                InstallHookDesktopSwitch();
            }
            internal void UninstallHook()
            {
                if(_hookID != nint.Zero)
                {
                    _ = UnhookWindowsHookEx(_hookID);
                    _hookID = nint.Zero;
                }                               
                UnhookDesktopSwitchHook();

                KeyDownPhysicallyProcessed.Clear();
            }
            private void InstallHookDesktopSwitch()
            {
                if(_hDesktopSwitchHook != nint.Zero) return;
                _desktopSwitchDelegate = new WinEventDelegate(DesktopSwitchEventHandler);

                _hDesktopSwitchHook = SetWinEventHook(EVENT_SYSTEM_DESKTOPSWITCH, EVENT_SYSTEM_DESKTOPSWITCH, nint.Zero, _desktopSwitchDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
                if(_hDesktopSwitchHook == nint.Zero) throw new Win32Exception();
            }
            private void UnhookDesktopSwitchHook()
            {
                if(_hDesktopSwitchHook == nint.Zero) return;
                _ = UnhookWinEvent(_hDesktopSwitchHook);
                _hDesktopSwitchHook = nint.Zero;
            }
            private nint LowLevelKeyboardProc(int nCode, WMEvent wParam, nint lParam)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                nint KeyDown(ref readonly TagKBDLLHOOKSTRUCT keyboardStruct)
                {   
                    KeyboardEventArgs keyboardEventDown = new KeyboardEventArgs(KeyboardEventArgs.TypePhysicallyEvent.Down, isDownRepeat: KeyDownPhysicallyProcessed.Contains(keyboardStruct.VkCode))
                    {
                        Key = ref keyboardStruct.VkCode
                    };

                    if(keyboardStruct.VkCode == VKeys.VK_DELETE) return (nint)1;

                    KeyAction!.Invoke(ref keyboardEventDown);

                    if(keyboardEventDown.BreakLogicKey is true) return (nint)1;

                    _ = KeyDownPhysicallyProcessed.Add(keyboardStruct.VkCode);

                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                nint KeyUP(ref readonly TagKBDLLHOOKSTRUCT keyboardStruct)
                {
                    if(keyboardStruct.VkCode == VKeys.VK_DELETE) return (nint)1;
                    KeyboardEventArgs keyboardEventUP = new KeyboardEventArgs(KeyboardEventArgs.TypePhysicallyEvent.Up)
                    {
                        Key = ref keyboardStruct.VkCode,
                    };

                    KeyAction!.Invoke(ref keyboardEventUP);

                    if(keyboardEventUP.BreakLogicKey is true) 
                    {
                        _ = KeyDownPhysicallyProcessed.Remove(keyboardStruct.VkCode);
                        return CallNextHookEx(_hookID, nCode, wParam, lParam); // Ошибки связанные Up фактически блокируют клавишу в зажатом состоянии. 
                    }

                    _ = KeyDownPhysicallyProcessed.Remove(keyboardStruct.VkCode);
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                if(nCode is HC_ACTION)
                {
                    TagKBDLLHOOKSTRUCT tagKBDLLHOOKSTRUCT = Marshal.PtrToStructure<TagKBDLLHOOKSTRUCT>(lParam);

                    switch(wParam)
                    {
                        case WMEvent.WM_KEYDOWN: return KeyDown(ref tagKBDLLHOOKSTRUCT);
                        case WMEvent.WM_SYSKEYDOWN: return KeyDown(ref tagKBDLLHOOKSTRUCT);
                        case WMEvent.WM_KEYUP: return KeyUP(ref tagKBDLLHOOKSTRUCT);
                        case WMEvent.WM_SYSKEYUP: return KeyUP(ref tagKBDLLHOOKSTRUCT);
                    }
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            private void DesktopSwitchEventHandler(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) => EnsureKeyboardStateSync();           
            private void EnsureKeyboardStateSync()
            {
                bool isForce = false;
                foreach(VKeys vkCodeDown in KeyDownPhysicallyProcessed)
                {
                    if((GetAsyncKeyState((int)vkCodeDown) & 0x8000) is 0)
                    {
                        _ = KeyDownPhysicallyProcessed.Remove(vkCodeDown);
                        isForce = true;
                    }
                }
                if(isForce is true)
                {
                    VKeys vKeys = VKeys.VK_F24;
                    KeyboardEventArgs keyboardEventDown = new KeyboardEventArgs(KeyboardEventArgs.TypePhysicallyEvent.ForceClearState, isDownRepeat:false)
                    {
                        Key = ref vKeys
                    };
                    KeyAction!.Invoke(ref keyboardEventDown);
                }
            }
            private enum WMEvent : uint
            {
                WM_KEYDOWN = 256,
                WM_SYSKEYDOWN = 260,
                WM_KEYUP = 257,
                WM_SYSKEYUP = 261,
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

            [LibraryImport("user32")]
            [return:MarshalAs(UnmanagedType.Bool)]
            private static partial bool UnhookWinEvent(nint hWinEventHook);
            [LibraryImport("user32")]
            private static partial nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
            [LibraryImport("user32")] 
            private static partial short GetAsyncKeyState(int vKey);
        }
    }
   public ref struct KeyboardEventArgs
   {
        internal enum TypePhysicallyEvent
        {
            Down = 1,
            Up = 2,
            ForceClearState
        }
        internal KeyboardEventArgs(TypePhysicallyEvent ev, bool breakLogicKey = false, bool isDownRepeat = false)
        {
            Type = ev;
            BreakLogicKey = breakLogicKey;
            IsDownRepeat = isDownRepeat;
        }
        internal ref readonly VKeys Key;
        internal readonly TypePhysicallyEvent Type;
        internal readonly bool IsDownRepeat;
        internal bool BreakLogicKey;
   }
}