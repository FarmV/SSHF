using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;


using FVH.Background.Input.Infrastructure.Interfaces;
using System.Windows;
using System.Threading;
using FVH.Background.Input.Infrastructure;
using static FVH.Background.Input.CallbackFunctionKeyboard;
using System.Runtime.InteropServices;

namespace FVH.Background.Input
{
    public partial class Input : IDisposable
    {
        /// <summary>
        /// Примечание: WS_POPUP в Windows API обычно представлен как 32-битное значение (Int32),
        /// но для ясности и соответствия документации используется тип long (Int64).
        /// При приведении значения к типу int происходит потеря старших битов, но это безопасно,
        /// так как младшие 32 бита достаточны для представления стиля окна WS_POPUP.
        /// </summary>
        private const long WS_POPUP = 0x80000000L;
        private const int WM_INPUT = 0x00FF;
        private const int THREAD_PRIORITY_TIME_CRITICAL = 15;
        private volatile bool _isDispose = false;
        private readonly Dispatcher _toCallbackDispatcher;
        private readonly Dispatcher _inputDispatcher;
        private readonly CallbackFunctionKeyboard _callbackFunctionKeyboard;
        private readonly SemaphoreSlim _semaphoreHook = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        internal event LowLevelKeyboard.KeyboardEventHandler? NotifyKeyboardEvent;
        public Input(Dispatcher toCallbackDispatcher)
        {
            _toCallbackDispatcher = toCallbackDispatcher;
            _inputDispatcher = CreateDispatcher();
            _callbackFunctionKeyboard = _inputDispatcher.Invoke(() => new CallbackFunctionKeyboard(_toCallbackDispatcher));
            _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.NotifyKeyboardEvent += SendNotifyKeyboardEvent);
        }
        private void SendNotifyKeyboardEvent(ref KeyboardEventArgs e) => NotifyKeyboardEvent?.Invoke(ref e);
        ~Input() => Dispose();
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = true;
            _inputDispatcher.Invoke(() =>
            {
                _callbackFunctionKeyboard?.Dispose();
            });
            _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.NotifyKeyboardEvent -= SendNotifyKeyboardEvent);
            _inputDispatcher.InvokeShutdown();
            GC.SuppressFinalize(this);
        }
        public void InstallHookToInputDispatcher()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            _semaphoreHook.Wait();
            try { _inputDispatcher.Invoke(_callbackFunctionKeyboard.InstallHook); } 
            finally { _ = _semaphoreHook.Release(); }
        }
        public void UninstallHookToInputDispatcher()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            _semaphoreHook.Wait();
            try { _inputDispatcher.Invoke(_callbackFunctionKeyboard.UninstallHook); }
            finally { _ = _semaphoreHook.Release(); }
        }
        public Task<bool> ContainsKeyCombination(VKeys[] keyCombo) => _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.ContainsKeyCombination(keyCombo));
        public Task AddCallbackTask(VKeys[] keyCombo, Func<Task> callbackTask, object? identifier = null, Func<bool>? canExecute = null) => _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.AddCallbackTask(keyCombo, callbackTask, identifier, canExecute));
        public Task<bool> DeleteTaskByAnIdentifier(object identifier) => _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.DeleteTaskByAnIdentifier(identifier));
        public Task<bool> DeleteInvokeListByKeyCombination(VKeys[] keyCombo) => _inputDispatcher.Invoke(() => _callbackFunctionKeyboard.DeleteInvokeListByKeyCombination(keyCombo));
        public List<GroupFunctions> ReturnGroupRegFunctions() => _inputDispatcher.Invoke(_callbackFunctionKeyboard.ReturnGroupRegFunctions);
        private static Dispatcher CreateDispatcher()
        {
            Thread? thread = null;
            Task InitThreadAndSetWindowsHandler = Task.Run(() =>
            {
                thread = new Thread(() => Dispatcher.Run())
                {
                    Name = ".FVH Background Input Handler"
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = false;

                thread.UnsafeStart();
            });
            Task<Dispatcher> waitForDispatcherValidation = Task.Run(async () =>
            {
                Dispatcher? winDispatcher = null;

                if (SpinWait.SpinUntil(() =>
                {
                    winDispatcher = Dispatcher.FromThread(thread);
                    return winDispatcher is not null;
                }, TimeSpan.FromMilliseconds(500)) is false) throw new NullReferenceException($"Failed to get window Dispatcher - {nameof(winDispatcher)} is null");

                bool isTimeoutInitializationDispatcher = false;
                System.Threading.Timer timeoutTimer = new System.Threading.Timer((_) => isTimeoutInitializationDispatcher = true);
                timeoutTimer.Change(TimeSpan.FromSeconds(4), Timeout.InfiniteTimeSpan);
                while (true)
                {
                    try
                    {
                        if (isTimeoutInitializationDispatcher is true) throw new TimeoutException(nameof(waitForDispatcherValidation));
                        Task taskWinInit = await winDispatcher!.InvokeAsync(async () => await Task.Delay(1)).Task;
                        timeoutTimer.Dispose();
                        break;
                    }
                    catch (System.Threading.Tasks.TaskCanceledException) { }
                }
                return winDispatcher;
            });

            Task.WaitAll(InitThreadAndSetWindowsHandler, waitForDispatcherValidation);

            Dispatcher.FromThread(thread).Invoke(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                nint hThread = GetCurrentThread();
                _ = SetThreadPriority(hThread, THREAD_PRIORITY_TIME_CRITICAL);
            });

            return waitForDispatcherValidation.Result;
        }        
        [LibraryImport("Kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetThreadPriority(nint hThread,int nPriority);
        [LibraryImport("Kernel32")]
        private static partial nint GetCurrentThread();
    }
}