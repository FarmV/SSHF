using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Controls.Ribbon;
using System.Windows.Threading;

namespace FVH.SSHF.Infrastructure.Win32
{
    internal sealed partial class Win32ExclusiveModeChecker : IDisposable
    {
        private static readonly Guid CLSID_DirectDraw7 = new Guid("3C305196-50DB-11D3-9CFE-00C04FD930C5");
        private static readonly Guid IID_IDirectDraw7 = new Guid("15E65EC0-3B9C-11D2-B92F-00609797EA5B");
        private const int DD_OK = 0;
        private const int DDERR_EXCLUSIVEMODEALREADYSET = unchecked((int)0x88760245);
        private bool _disposed = false;
        private readonly Windows.Win32.Graphics.DirectDraw.IDirectDraw7 _idd7;
        internal Win32ExclusiveModeChecker()
        {
            if(Type.GetTypeFromCLSID(CLSID_DirectDraw7) is not Type directDraw7) ThrowGetType(); [DoesNotReturn] static void ThrowGetType() => throw new ArgumentNullException(nameof(directDraw7));
            if(Activator.CreateInstance(directDraw7) is not Windows.Win32.Graphics.DirectDraw.IDirectDraw7 iDirectDraw7) ThrowCreateInstance(); [DoesNotReturn] static void ThrowCreateInstance() => throw new ArgumentNullException(nameof(iDirectDraw7));
            Guid emptyInitialize = Guid.Empty;
            Windows.Win32.Foundation.HRESULT resultInitialize = Windows.Win32.Graphics_DirectDraw_IDirectDraw7_Extensions.Initialize(iDirectDraw7, ref emptyInitialize);
            if(resultInitialize != DD_OK) Marshal.ThrowExceptionForHR(resultInitialize);

            _idd7 = iDirectDraw7;
        }
        ~Win32ExclusiveModeChecker() => Dispose(false);      
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if(_disposed) return;
            _disposed = true;

            if(disposing is true) { /*Managed Resources*/ }
            
            if(_idd7 is not null) _ = Marshal.ReleaseComObject(_idd7);           
        }
        internal bool CheckExclusiveMode(Dispatcher staDispatcher) => staDispatcher.Invoke(() => _idd7.TestCooperativeLevel() == DDERR_EXCLUSIVEMODEALREADYSET);             

        [LibraryImport("Gdi32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool D3DKMTCheckExclusiveOwnership();
    }   
}