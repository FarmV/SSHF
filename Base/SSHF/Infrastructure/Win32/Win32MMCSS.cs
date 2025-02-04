using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class Win32MMCSS : IDisposable
    {
        private const string _nameCategoryTask = "Games";
        private const int AVRT_PRIORITY_HIGH = 1;
        private nint _threadAssociateTaskWindows = nint.Zero;
        private bool _isDispose = false;
        private bool _inTimeCriticalSection = false;
        private readonly Dispatcher _dispatcher;
        public Win32MMCSS(Dispatcher uiDispatcher) 
        { 
           _dispatcher = uiDispatcher;          
        } 
        public void Dispose()
        {
            if(_isDispose is true) return;
            _isDispose = false;
            _ = StopTimeCriticalSectionUI();
            _threadAssociateTaskWindows = default;
        }
        public bool InTimeCriticalSection { get => _inTimeCriticalSection; }
        internal bool StartTimeCriticalSectionUI()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            if(_dispatcher.Thread != Thread.CurrentThread) throw new InvalidOperationException();
            if(_inTimeCriticalSection is true) return true; //todo логирование ошибки
            bool result = false;
            _threadAssociateTaskWindows = AvSetMmThreadCharacteristicsW(_nameCategoryTask, out _);
#if DEBUG
            #region DEBUG
            if(App.Trace.Level is not TraceLevel.Off)
            {
                if(_threadAssociateTaskWindows == nint.Zero)
                {
                    if(App.Trace.Level == TraceLevel.Error) Debug.WriteLine(
                    message: $"{nameof(_threadAssociateTaskWindows)}, TraceLevel - {TraceLevel.Error} => {nameof(result)} = {result}",
                    category: $"{typeof(Win32MMCSS)}.{nameof(StartTimeCriticalSectionUI)}");
                }

            }
            #endregion

#endif
            if(_threadAssociateTaskWindows != nint.Zero)
            {
#if DEBUG
                #region DEBUG
                if(App.Trace.Level is not TraceLevel.Off)
                {
                    if(App.Trace.Level == TraceLevel.Verbose) Debug.WriteLine(
                    message: $"{nameof(_threadAssociateTaskWindows)}, TraceLevel - {TraceLevel.Verbose} => {nameof(result)} = {true}",
                    category: $"{typeof(Win32MMCSS)}.{nameof(StartTimeCriticalSectionUI)}");
                }
                #endregion
#endif         
                _ =  AvSetMmThreadPriority(_threadAssociateTaskWindows, AVRT_PRIORITY_HIGH); //toto логирование
                _inTimeCriticalSection = true;
                result = true;
            }
            return result;
        }
        internal bool StopTimeCriticalSectionUI()
        {
            ObjectDisposedException.ThrowIf(_isDispose, this);
            if(_dispatcher.Thread != Thread.CurrentThread) throw new InvalidOperationException();
            if(_inTimeCriticalSection is false)
            {
#if DEBUG
                #region DEBUG
                if(App.Trace.Level is not TraceLevel.Off)
                {
                    if(App.Trace.Level >= TraceLevel.Warning) Debug.WriteLine(
                    message: $"{nameof(_inTimeCriticalSection)}, TraceLevel - {TraceLevel.Warning} => {nameof(_inTimeCriticalSection)} = {false}",
                    category: $"{typeof(Win32MMCSS)}.{nameof(StopTimeCriticalSectionUI)}");

                }
                #endregion
#endif
                return true;
            }
            if(_threadAssociateTaskWindows == nint.Zero)
            {
#if DEBUG
                #region DEBUG
                if(App.Trace.Level is not TraceLevel.Off)
                {
                    if(App.Trace.Level >= TraceLevel.Info) Debug.WriteLine(
                    message: $"{nameof(_threadAssociateTaskWindows)}, TraceLevel - {TraceLevel.Warning} => {nameof(_threadAssociateTaskWindows)} = {nint.Zero}",
                    category: $"{typeof(Win32MMCSS)}.{nameof(StopTimeCriticalSectionUI)}");
                }
                #endregion
#endif
                return false;
            }
            bool resultSetAvRevertMmThreadCharacteristics = AvRevertMmThreadCharacteristics(_threadAssociateTaskWindows);
            _inTimeCriticalSection = false;
#if DEBUG
            #region DEBUG
            if(App.Trace.Level is not TraceLevel.Off)
            {
                if(App.Trace.Level >= TraceLevel.Verbose) Debug.WriteLine(
                message: $"{nameof(_inTimeCriticalSection)}, TraceLevel - {TraceLevel.Verbose} => {nameof(resultSetAvRevertMmThreadCharacteristics)} = {resultSetAvRevertMmThreadCharacteristics}",
                category: $"{typeof(Win32MMCSS)}.{nameof(StopTimeCriticalSectionUI)}");
            }
            #endregion
#endif

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
