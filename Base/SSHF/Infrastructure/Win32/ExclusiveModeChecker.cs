using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Graphics.DirectDraw;

using HRESULT = Windows.Win32.Foundation.HRESULT;


namespace FVH.SSHF.Infrastructure
{
    internal class ExclusiveModeChecker()
    {
        internal static Guid CLSID_DirectDraw7 = new Guid("3C305196-50DB-11D3-9CFE-00C04FD930C5");
        internal static Guid IID_IDirectDraw7 = new Guid("15E65EC0-3B9C-11D2-B92F-00609797EA5B");

        private const int _S_OK = 0;
        private const int _DD_OK = 0;
        private const int _DDERR_EXCLUSIVEMODEALREADYSET = unchecked((int)0x88760245);
        private const int _CLSCTX_INPROC_SERVER = 0x1;

        internal bool CheckExclusiveMode(Dispatcher staDispatcher)
        {
            bool isExclusiveMode = false;
            staDispatcher.Invoke(() =>
            {
                IDirectDraw7? dd7 = null;
                try
                {
                    int resultCoCreateInstance = CoCreateInstance(ref CLSID_DirectDraw7, null, _CLSCTX_INPROC_SERVER, ref IID_IDirectDraw7, out nint pDD7);
                    if(resultCoCreateInstance is not _S_OK) Marshal.ThrowExceptionForHR(resultCoCreateInstance);

                    Guid emptyInitialize = Guid.Empty;

                    dd7 = (IDirectDraw7)Marshal.GetObjectForIUnknown(pDD7);
                    HRESULT resultInitialize = dd7.Initialize(ref emptyInitialize);
                    if(resultInitialize != _DD_OK) Marshal.ThrowExceptionForHR(resultInitialize);
                    HRESULT resultTestCooperativeLevel = dd7.TestCooperativeLevel();

                    if(resultTestCooperativeLevel == _DDERR_EXCLUSIVEMODEALREADYSET) isExclusiveMode = true;
                }
                finally { if(dd7 is not null) Marshal.ReleaseComObject(dd7); }
            });
            return isExclusiveMode;
        }
        [DllImport("ole32")]
        private static extern int CoCreateInstance(ref Guid rclsid, [MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, uint dwClsContext, ref Guid riid, out nint ppv);
    }
}

