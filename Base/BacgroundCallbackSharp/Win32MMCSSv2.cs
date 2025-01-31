using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FVH.Background.Input
{
    internal partial class Win32MMCSSv2: IDisposable
    {
        private readonly string _nameCategoryTask = "DisplayPostProcessing";
        private nint _threadAssociateTaskWindows = nint.Zero;
        private bool _isDispose = false;
        private Dispatcher _dispatcher;
        private bool _isMaxCPUPriority = false;
        public Win32MMCSSv2(Dispatcher uiDispatcher) => _dispatcher = uiDispatcher;      
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = false;
            _ = SetNormalCPUPriority();
            _threadAssociateTaskWindows = default;
        }
        public bool IsMaxCPUPriority { get => _isMaxCPUPriority; }
        internal bool SetMaxCPUPriority()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            if(_isMaxCPUPriority is true) return true;
            bool result = false;
            _threadAssociateTaskWindows = AvSetMmThreadCharacteristicsW(_nameCategoryTask, out _);

            if(_threadAssociateTaskWindows != nint.Zero)
            {    
                _isMaxCPUPriority = true;

                if(AvSetMmThreadPriority(_threadAssociateTaskWindows, -2) is not true) throw new InvalidOperationException();

                result = true;
            }
            return result;
        }
        internal bool SetNormalCPUPriority()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            if(_isMaxCPUPriority is false)
            {


              return true;
            }
            if(_threadAssociateTaskWindows == nint.Zero)
            {

                return false;
            }
            bool resultSetAvRevertMmThreadCharacteristics = AvRevertMmThreadCharacteristics(_threadAssociateTaskWindows);
            _isMaxCPUPriority = false;


            return resultSetAvRevertMmThreadCharacteristics;
        }
        [LibraryImport("Avrt")]
        [return: MarshalAs(UnmanagedType.SysInt)]
        private static partial nint AvSetMmThreadCharacteristicsW([MarshalAs(UnmanagedType.LPWStr)] string taskName, out uint taskIndex);
        [LibraryImport("Avrt")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AvRevertMmThreadCharacteristics(nint AvrtHandle);
        [LibraryImport("Avrt")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AvSetMmThreadPriority(nint AvrtHandle, int Priority);
    }
}
