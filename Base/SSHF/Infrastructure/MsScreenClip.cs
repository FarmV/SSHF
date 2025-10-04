using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

using R3;


namespace FVH.SSHF.Infrastructure
{  
    public unsafe sealed partial class MsScreenClip : IDisposable
    {
        private const           int                                   S_OK                               = 0x00000000;
        private const           string                                _uriScheme                         = "ms-screenclip:";
        private const           string                                _processName                       = "ScreenClippingHost";
        private static readonly Guid                                  CLSID_ApplicationActivationManager = new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C");
        private static readonly Guid                                  IID_IApplicationActivationManager  = typeof(IApplicationActivationManager).GUID;
        private        readonly IApplicationActivationManager.Native* _pApplicationActivationManager     = null;
        private                 bool                                  _disposed                          = false;
        private                 StrategyBasedComWrappers              _localComWrappers;
        private                 IApplicationActivationManager         _IApplicationActivationManager;

        private const string _win10AppUserModelID = "MicrosoftWindows.Client.CBS_cw5n1h2txyewy!ScreenClipping";
        private const string _win11AppUserModelID = "MicrosoftWindows.Client.Core_cw5n1h2txyewy!ScreenClipping";

        private readonly HashSet<uint> _trackedProcessIds = new();
        private readonly Lock          _trackerLock = new();

        private readonly BehaviorSubject<bool> _isClipping;
        public ReadOnlyReactiveProperty<bool> IsClipping { get; }
        public MsScreenClip()
        {
            _isClipping = new BehaviorSubject<bool>(false);
            IsClipping  = _isClipping.ToBindableReactiveProperty();

            IApplicationActivationManager.Native* pApplicationActivationManager = null;
            const nint NoAggregation = 0;
            int hrCoCreateInstance;
            fixed(Guid* pClsid = &CLSID_ApplicationActivationManager, pIid = &IID_IApplicationActivationManager)
            {
                hrCoCreateInstance = CoCreateInstance(pClsid, (IUnknown.Native*)NoAggregation, CLSCTX_INPROC_SERVER, pIid, (IUnknown.Native**)&pApplicationActivationManager);
            }
            if(hrCoCreateInstance < S_OK) Throw(hrCoCreateInstance); [DoesNotReturn] static void Throw(int hr) => throw Marshal.GetExceptionForHR(hr) ?? new System.Runtime.InteropServices.COMException(null, hr);
            _pApplicationActivationManager = pApplicationActivationManager;
            _localComWrappers = new StrategyBasedComWrappers();

            _IApplicationActivationManager = (IApplicationActivationManager)_localComWrappers.GetOrCreateObjectForComInstance((nint)_pApplicationActivationManager, CreateObjectFlags.None);
          
        }

        public void Invoke()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
            using(_trackerLock.EnterScope())
            {
                if(_trackedProcessIds.Count > 0)
                {
                    bool r = IsEnableProcessHost();
                    return;
                } 
            }
            if(IsEnableProcessHost() is true) return;

            uint processId = default;

            const string? argEmpty = null;
            const int     osFirstBuildWin11 = 22000;
            if(App.OperatingSystem.Version.Build < osFirstBuildWin11) _IApplicationActivationManager.ActivateApplication(_win10AppUserModelID, argEmpty, IApplicationActivationManager.ActivateOptions.None, out processId);            
            else { _IApplicationActivationManager.ActivateApplication(_win11AppUserModelID, argEmpty, IApplicationActivationManager.ActivateOptions.None, out processId); }

            if(processId == default)
            {
#if DEBUG
                if(System.Diagnostics.Debugger.IsAttached is true) Debugger.Break();
                else { Debug.WriteLine($"Warning: {nameof(Invoke)} result: processID is 0"); }
#endif
                return;
            }
            using(_trackerLock.EnterScope())
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
                if(_trackedProcessIds.Add(processId) is false) return;
                
                if(_trackedProcessIds.Count == 1)
                {
                    _isClipping.OnNext(true);
                }
            }

            ProcessLifetimeTracker tracker = new ProcessLifetimeTracker();
            _ = tracker.TrackProcessAsync(processId)
           .ContinueWith(task =>
           {
               if(Volatile.Read(ref _disposed) is false)
               {
                   using(_trackerLock.EnterScope())
                   {
                       _trackedProcessIds.Remove(processId);

                       if(_trackedProcessIds.Count == 0)
                       {
                           _isClipping.OnNext(false);
                       }
                   }
               }
               tracker.Dispose();

               if(task.IsFaulted) ExceptionDispatchInfo.Capture(task.Exception!.InnerException!).Throw();

           }, TaskScheduler.Default);
        }
        public void Dispose()
        {
            if(Interlocked.CompareExchange(ref _disposed, true, false) is true) return;

            using(_trackerLock.EnterScope())
            {
                _trackedProcessIds.Clear(); 
            }
            _isClipping.Dispose();

            if(_pApplicationActivationManager is not null) _ = Marshal.Release((nint)_pApplicationActivationManager);
            _localComWrappers = null!;
            _IApplicationActivationManager = null!;
        }

        internal void SetStateIfWindowScreenclip(nint handleWindow)
        {
            if(Volatile.Read(ref _disposed) is true) return;
            _ = GetWindowThreadProcessId(handleWindow,out uint processId);
            if(processId == default) return;

            if(IsEnableProcessHost(processId) is false) return;

            using(_trackerLock.EnterScope())
            {
                if(Volatile.Read(ref _disposed) is true) return;
                if(_trackedProcessIds.Add(processId) is false) return; 

                if(_trackedProcessIds.Count == 1)
                {
                    _isClipping.OnNext(true);
                }
            }

            ProcessLifetimeTracker tracker = new ProcessLifetimeTracker();
            _ = tracker.TrackProcessAsync(processId)
            .ContinueWith(task =>
            {
                if(Volatile.Read(ref _disposed) is false) // ← Проверка disposed
                {
                    using(_trackerLock.EnterScope())
                    {
                        _trackedProcessIds.Remove(processId);

                        if(_trackedProcessIds.Count == 0)
                        {
                            _isClipping.OnNext(false);
                        }
                    }
                }

                tracker.Dispose();

                if(task.IsFaulted) ExceptionDispatchInfo.Capture(task.Exception!.InnerException!).Throw();

            }, TaskScheduler.Default);
        }
        private static bool IsEnableProcessHost()
        {
            Process[] msScreenClipProc = Array.Empty<Process>();
            try
            {
                msScreenClipProc = System.Diagnostics.Process.GetProcessesByName(_processName);

                if(msScreenClipProc.Length is 0) return false;
                if(msScreenClipProc.Length > 1)
                {
#if DEBUG
                    if(System.Diagnostics.Debugger.IsAttached is true) Debugger.Break();
                    else { Debug.WriteLine($"Warning: Multiple {_processName} processes found: {msScreenClipProc.Length}"); }
#endif
                    return true;
                }
                return msScreenClipProc[0].HasExited is false;
            }
            catch(InvalidOperationException)
            {
                return false;
            }
            finally
            {
                foreach(Process? proc in msScreenClipProc)
                {
                    proc?.Dispose();
                }
            }

        }
        private static bool IsEnableProcessHost(uint pid)
        {
            try
            {
                using Process process = System.Diagnostics.Process.GetProcessById((int)pid);
                if(process.MainModule is null) return false;

                return process.MainModule.FileName.Contains("ScreenClippingHost");
            }
            catch(ArgumentException) // Process not found
            {
                return false;
            }
            catch(InvalidOperationException) // Process already exited or no access
            {
                return false;
            }
        }
        [GeneratedComInterface, System.Runtime.InteropServices.Guid("00000000-0000-0000-C000-000000000046")]
        internal partial interface IUnknown { public struct Native { } }
        [GeneratedComInterface, System.Runtime.InteropServices.Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
        internal partial interface IApplicationActivationManager : IUnknown
        {
            [PreserveSig] public void ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,[MarshalAs(UnmanagedType.LPWStr)] string? arguments, ActivateOptions options, out uint processId);
            [PreserveSig] public void ActivateForFile([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,nint /* IShellItemArray* */ itemArray,[MarshalAs(UnmanagedType.LPWStr)] string? verb,out uint processId);
            [PreserveSig] public void ActivateForProtocol([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,nint /* IShellItemArray* */ itemArray,out uint processId);
            public new struct Native { }
            [Flags]
            public enum ActivateOptions : uint
            {
                None           = 0x00000000,
                DesignMode     = 0x00000001,
                NoErrorUI      = 0x00000002,
                NoSplashScreen = 0x00000004,
                Prelaunch      = 0x20000000,
            }
        }
        private const uint CLSCTX_INPROC_SERVER = 0x1;
        [LibraryImport("ole32")] private static partial int CoCreateInstance(Guid* rclsid, IUnknown.Native* pUnkOuter, uint dwClsContext, Guid* riid, IUnknown.Native** ppv);
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_READ = 0x0010;
        [LibraryImport("kernel32")] private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)]bool bInheritHandle, uint dwProcessId);
        [LibraryImport("kernel32")] [return: MarshalAs(UnmanagedType.Bool)] private static partial bool CloseHandle(nint hObject);
        private const int  WT_EXECUTEONLYONCE = 0x00000008;

        [LibraryImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool RegisterWaitForSingleObject(nint* phNewWaitObject, nint hObject, delegate* unmanaged<nint, byte, void> Callback, nint Context, uint dwMilliseconds, uint dwFlags);
        [LibraryImport("user32")]
        private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
        internal unsafe sealed partial class ProcessLifetimeTracker : IDisposable
        {
            private readonly Lock                       _lock = new();
            private          TaskCompletionSource<bool> _tcs;
            private          nint                       _processHandle;
            private          nint                       _waitHandle;
            private          GCHandle                   _gcHandle;
            private const    nint                       unregisterNoWait = 0;
            public ProcessLifetimeTracker()
            {
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _tcs.SetResult(true);
            }
            public Task TrackProcessAsync(uint processId)
            {
                if(processId == default) return HandleInvalidArgument(); static Task HandleInvalidArgument() => Task.FromException(new ArgumentException("Process ID cannot be zero.", nameof(processId)));
                
                using(_lock.EnterScope())
                {
                    if(_processHandle != nint.Zero) return HandleAlreadyTracking(); static Task HandleAlreadyTracking() => Task.FromException(new InvalidOperationException("This tracker is already tracking a process."));

                    _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                    nint hProcess = OpenProcess(PROCESS_SYNCHRONIZE, false, processId);
                    if(hProcess == nint.Zero)
                    {
                        HandleOpenProcessFailed(ref _tcs); static void HandleOpenProcessFailed(ref TaskCompletionSource<bool> tcs) => _ = tcs.TrySetException(new Win32Exception("Failed to open process. It may have already exited or access was denied."));
                        return _tcs.Task;
                    }

                    nint waitHandleLocal = default;
                    GCHandle gcHandle    = GCHandle.Alloc(this);
                    bool success         = false;

                    try { success = RegisterWaitForSingleObject(&waitHandleLocal, hProcess, &ProcessExitedCallback, GCHandle.ToIntPtr(gcHandle), uint.MaxValue, WT_EXECUTEONLYONCE); }
                    finally
                    {
                        if(success is false && gcHandle.IsAllocated) gcHandle.Free();
                    }

                    if(success is false)
                    {
                        static Task HandleRegisterWaitFailed(ref TaskCompletionSource<bool> tcs, nint hProcess)
                        {
                            Win32Exception ex = new("RegisterWaitForSingleObject failed.");
                            _ = CloseHandle(hProcess);
                            _ = tcs.TrySetException(ex);
                            return tcs.Task;
                        }
                        return HandleRegisterWaitFailed(ref _tcs, hProcess);                    
                    }
                    else
                    {
                        _processHandle = hProcess;
                        _waitHandle = waitHandleLocal;
                        _gcHandle = gcHandle;
                    }
                }
                return _tcs.Task;
            }
            
            private void OnProcessCompleted()
            {
                nint processHandleToClose   = nint.Zero;
                nint waitHandleToUnregister = nint.Zero;

                using(_lock.EnterScope())
                {
                    if(_processHandle == nint.Zero) return;
                    processHandleToClose   = _processHandle;
                    waitHandleToUnregister = _waitHandle; 

                    _processHandle = nint.Zero;
                    _waitHandle = nint.Zero;

                    if(_gcHandle.IsAllocated) _gcHandle.Free();
                }

                if(processHandleToClose   != nint.Zero) _ = CloseHandle(processHandleToClose);
                _ = _tcs.TrySetResult(true);

                if(waitHandleToUnregister != nint.Zero) _ = UnregisterWaitEx(waitHandleToUnregister, unregisterNoWait);
            }
            [UnmanagedCallersOnly]
            private static void ProcessExitedCallback(nint lpParameter, byte timerOrWaitFired)
            {
                GCHandle gcHandle = default;
                try
                {
                    gcHandle = GCHandle.FromIntPtr(lpParameter);
                    if(gcHandle.Target is ProcessLifetimeTracker instance)
                    {
                        instance.OnProcessCompleted();
                    }
                }
                catch(Exception ex)
                {
                    if(gcHandle.IsAllocated && gcHandle.Target is ProcessLifetimeTracker instance)
                    {
                        _ = instance._tcs.TrySetException(ex);
                    }
                }
            }
            public void Dispose()
            {
                nint processHandleToClose;
                nint waitHandleToUnregister;

                using(_lock.EnterScope())
                {
                    if(_processHandle == nint.Zero) return;

                    processHandleToClose = _processHandle;
                    waitHandleToUnregister = _waitHandle;

                    _processHandle = nint.Zero;
                    _waitHandle = nint.Zero;

                    if(_gcHandle.IsAllocated) _gcHandle.Free();
                }

                _ = _tcs.TrySetCanceled();
                 const nint INVALID_HANDLE_VALUE = (nint)(-1); // ожидаем завершения callback
                if(waitHandleToUnregister != nint.Zero) _ = UnregisterWaitEx(waitHandleToUnregister, INVALID_HANDLE_VALUE);
                if(processHandleToClose   != nint.Zero) _ = CloseHandle(processHandleToClose);
            }
   
            private const uint PROCESS_SYNCHRONIZE = 0x00100000;
            private const uint WT_EXECUTEONLYONCE  = 0x00000008;
            [LibraryImport("kernel32")] private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);
            [LibraryImport("kernel32")] [return: MarshalAs(UnmanagedType.Bool)] private static partial bool CloseHandle(nint hObject);
            [LibraryImport("kernel32")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool RegisterWaitForSingleObject(nint* phNewWaitObject, nint hObject, delegate* unmanaged<nint, byte, void> Callback, nint Context, uint dwMilliseconds, uint dwFlags);
            [LibraryImport("kernel32")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool UnregisterWaitEx(nint WaitHandle, nint CompletionEvent);
        }
    }
}