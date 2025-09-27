using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using R3;

using Windows.Win32.Foundation;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class ObserverMsScreenClipExecuting
    {
        private const string MsScreenClipPath = "C:\\Windows\\SystemApps\\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\\ScreenClippingHost.exe";
        private readonly HashSet<Process> _msScreenClipExecutingSet;
        internal readonly R3.BehaviorSubject<bool> IsExecutingProcessScreenClip;
        public ObserverMsScreenClipExecuting()
        {
            _msScreenClipExecutingSet = new HashSet<Process>();
            IsExecutingProcessScreenClip = new BehaviorSubject<bool>(false);
        }
        internal void CheckAndSetStateMsScreenClipExecuting(ref nint handleWindow)
        {
            void ProcessExitedEvent(object? proc, EventArgs __)
            {
                _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();
                if(proc is not Process pr) throw new InvalidCastException();
                pr.Exited -= ProcessExitedEvent;
                pr.Dispose();
                IsExecutingProcessScreenClip.OnNext(false);
            }

            uint procID = default;
            try { _ = GetWindowThreadProcessId(handleWindow, out procID); }
            catch { return; }
            if(procID == default) return;

            Process? process = null;

            try
            {
                process = System.Diagnostics.Process.GetProcessById((int)procID);
                if(process.MainModule is null)
                {
                    process.Dispose();
                    return;
                }
                if(process.MainModule.FileName == MsScreenClipPath)
                {
                    if(process.HasExited is true) return;
                    if(_msScreenClipExecutingSet.Contains(process) is true) return;
                    _ = _msScreenClipExecutingSet.Add(process);
                    process.EnableRaisingEvents = true;

                    process.Exited += ProcessExitedEvent;

                    IsExecutingProcessScreenClip.OnNext(true);
                }
            }
            catch(System.ComponentModel.Win32Exception) { process?.Dispose(); }
        }              
        [LibraryImport("user32")]
        private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
    }
}