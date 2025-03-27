using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            void ProcessExitedEvent(object? proc, EventArgs _)
            {
                if(proc is not Process pr) throw new InvalidCastException();
                pr.Exited -= ProcessExitedEvent;
                pr.Dispose();
                IsExecutingProcessScreenClip.OnNext(false);
            }
            _ = GetWindowThreadProcessId(new HWND(handleWindow), out uint procID);
            Process pr = System.Diagnostics.Process.GetProcessById((int)procID);
            if(pr.MainModule is null)
            {
                pr.Dispose();
                return;
            }
            if(pr.MainModule.FileName == MsScreenClipPath)
            {
                if(pr.HasExited is true) return;
                if(_msScreenClipExecutingSet.Contains(pr) is true) return;
                _ = _msScreenClipExecutingSet.Add(pr);
                pr.EnableRaisingEvents = true;

                pr.Exited += ProcessExitedEvent;

                IsExecutingProcessScreenClip.OnNext(true);
            }
        }              
        [DllImport("user32")]
        private static extern uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId);
    }
}