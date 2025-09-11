using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using static System.Runtime.InteropServices.ComWrappers;

namespace FVH.SSHF.Infrastructure
{
    internal sealed unsafe partial class VirtualFileDragDrop
    {
        private const int S_OK            = 0x00000000;
        private const int S_FALSE         = 0x1;
        private const int E_NOTIMPL       = unchecked((int)0x80004001);
        private const int E_POINTER       = unchecked((int)0x80004003);
        private const int E_FAIL          = unchecked((int)0x80004005);
        private const int DV_E_FORMATETC  = unchecked((int)0x80040064);
        private const int E_INVALIDARG    = unchecked((int)0x80070057);

        private static readonly ushort s_fileGroupDescriptorFormatId;
        private static readonly ushort s_fileContentsFormatId;

        private static readonly Guid IID_IEnumFORMATETC;
        private static readonly Guid IID_IDataObject;
        private static readonly Guid IID_IDropSource;
        private static readonly Guid IID_IStream;

        private const int  MAX_PATH        = 260;
        private const char NULL_TERMINATOR = '\0';

        private static readonly StrategyBasedComWrappers s_localComWrappers;

        static VirtualFileDragDrop()
        {
            IID_IEnumFORMATETC = typeof(IEnumFORMATETC).GUID;
            IID_IDataObject    = typeof(IDataObject).GUID;
            IID_IDropSource    = typeof(IDropSource).GUID;
            IID_IStream        = typeof(IStream).GUID;

            s_fileGroupDescriptorFormatId = (ushort)RegisterClipboardFormatW("FileGroupDescriptorW");
            s_fileContentsFormatId        = (ushort)RegisterClipboardFormatW("FileContents");

            s_localComWrappers = new StrategyBasedComWrappers();
        }
        public VirtualFileDragDrop() { }
        public unsafe void InitiateDrop(MemoryStream imageStream, string fileName)
        {
            DataObject dataObject = new DataObject(imageStream, fileName, s_localComWrappers);
            DropSource dropSource = new DropSource();

            nint pUnkDataObject = s_localComWrappers.GetOrCreateComInterfaceForObject(dataObject, CreateComInterfaceFlags.None);
            nint pUnkDropSource = s_localComWrappers.GetOrCreateComInterfaceForObject(dropSource, CreateComInterfaceFlags.None);

            if(pUnkDataObject == nint.Zero) ThrowArgumentNull(nameof(pUnkDataObject));
            if(pUnkDropSource == nint.Zero) ThrowArgumentNull(nameof(pUnkDropSource));

            IDataObject.Native* pDataObj = null;
            IDropSource.Native* pDropSrc = null;

            try
            {
                int hResultQI_DataObject;
                fixed(Guid* pIID = &IID_IDataObject)
                    hResultQI_DataObject = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)
                        ((void**)((ComInterfaceDispatch*)pUnkDataObject)->Vtable)[0])((ComInterfaceDispatch*)pUnkDataObject, pIID, (void**)&pDataObj);

                if(hResultQI_DataObject < 0) ThrowQueryInterface(nameof(IDataObject), hResultQI_DataObject);

                int hResultQI_DropSource;
                fixed(Guid* pIID = &IID_IDropSource)
                    hResultQI_DropSource = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)
                        ((void**)((ComInterfaceDispatch*)pUnkDropSource)->Vtable)[0])((ComInterfaceDispatch*)pUnkDropSource, pIID, (void**)&pDropSrc);

                if(hResultQI_DropSource < 0) ThrowQueryInterface(nameof(IDropSource), hResultQI_DropSource);

                const uint allowedEffects = (uint)DROPEFFECT.DROPEFFECT_COPY;
                uint performedEffect = 0;

                int hrDoDragDrop = DoDragDrop(pDataObj, pDropSrc, allowedEffects, &performedEffect);

                if(hrDoDragDrop < 0)
                {
#if DEBUG
                    if(Debugger.IsAttached)
                    {
                        if(hrDoDragDrop == E_POINTER)    Debugger.Break();
                        if(hrDoDragDrop == E_INVALIDARG) Debugger.Break();

                        Debugger.Break();
                    }
#endif
                    ThrowDoDragDrop(hrDoDragDrop);
                }
            }
            finally
            {
                if(pUnkDataObject != nint.Zero) _ = Marshal.Release(pUnkDataObject);
                if(pUnkDropSource != nint.Zero) _ = Marshal.Release(pUnkDropSource);
            }     
            [DoesNotReturn] static void ThrowArgumentNull(string pointerName)                  => throw new ArgumentNullException(pointerName, $"Failed to create COM interface pointer for {pointerName}.");
            [DoesNotReturn] static void ThrowQueryInterface(string interfaceName, int hResult) => throw new COMException($"QueryInterface failed for interface '{interfaceName}' with HRESULT: 0x{hResult:X8}.", hResult);
            [DoesNotReturn] static void ThrowDoDragDrop(int hResult)                           => throw new COMException($"DoDragDrop function failed with HRESULT: 0x{hResult:X8}.", hResult);
        }
        [GeneratedComClass]
        private unsafe partial class DataObject(MemoryStream imageStream, string fileName, StrategyBasedComWrappers localComWrappers) : IDataObject/*, ICustomQueryInterface*/
        {
            private const    int                       OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
            private readonly string                   _fileName         = fileName;
            private readonly MemoryStream             _imageStream      = imageStream;
            private readonly StrategyBasedComWrappers _localComWrappers = localComWrappers;

            public int EnumFormatEtc(uint dwDirection, IEnumFORMATETC.Native** ppenumFormatEtc)
            {
                if(ppenumFormatEtc is null) return E_POINTER;

                *ppenumFormatEtc = (IEnumFORMATETC.Native*)null;

                const uint DATADIR_GET = 1;
                if(dwDirection is not DATADIR_GET) return E_NOTIMPL;

                FORMATETC[] supportedFormats = new FORMATETC[]
                    {
                        // 1. "Манифест" (FileGroupDescriptorW).
                        new FORMATETC()
                        {
                            cfFormat = s_fileGroupDescriptorFormatId,
                            dwAspect = DVASPECT.DVASPECT_CONTENT,
                            lindex   = -1, // Относится ко всем файлам в группе
                            tymed    = TYMED.TYMED_HGLOBAL
                        },
                        // 2. "Содержимое" (FileContents).
                        new FORMATETC()
                        {
                            cfFormat = s_fileContentsFormatId,
                            dwAspect = DVASPECT.DVASPECT_CONTENT,
                            lindex   = 0,  // Относится к первому (и единственному) файлу
                            tymed    = TYMED.TYMED_ISTREAM
                        }
                    };

                EnumFormatEtc enumerator = new EnumFormatEtc(supportedFormats, _localComWrappers);

                nint pointerIUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(enumerator, CreateComInterfaceFlags.None);
                if(pointerIUnknown == nint.Zero) return E_FAIL;

                int hr;
                fixed(Guid* pIID = &IID_IEnumFORMATETC) hr = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)((void**)((ComInterfaceDispatch*)pointerIUnknown)->Vtable)[0])((ComInterfaceDispatch*)pointerIUnknown, pIID, (void**)ppenumFormatEtc);

                _ = Marshal.Release(pointerIUnknown);

                return hr;
            }
            public int GetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium)
            {
                if(pMedium is null) return E_POINTER;
                *pMedium = default;

                switch(pFormatetc->cfFormat)
                {
                    case ushort formatId when formatId == s_fileGroupDescriptorFormatId: return CreateFileGroupDescriptor(pMedium);
                    case ushort formatId when formatId == s_fileContentsFormatId:        return CreateFileContents(pMedium);
                    default: return DV_E_FORMATETC;
                }
            }
            public int QueryGetData(FORMATETC* pFormatetc) =>pFormatetc->cfFormat == s_fileGroupDescriptorFormatId || pFormatetc->cfFormat == s_fileContentsFormatId ? S_OK : DV_E_FORMATETC;                         
            public int GetDataHere(FORMATETC* pFormatetc, STGMEDIUM* pMedium) => S_OK;
            public int GetCanonicalFormatEtc(FORMATETC* pFormatetcIn, FORMATETC* pFormatetcOut)
            {
                if(pFormatetcOut is null) return E_POINTER;
                *pFormatetcOut = default;

                const int DATA_E_FORMATETC = unchecked((int)0x80040064);
                return DATA_E_FORMATETC;
            }
            public int SetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium, [MarshalAs(UnmanagedType.Bool)] bool fRelease) => _ = E_NOTIMPL;
            public int DAdvise(FORMATETC* pFormatetc, uint advf, IAdviseSink.NativeNotImplemented* pAdvSink, uint* pdwConnection)
            {
                if(pdwConnection is not null) *pdwConnection = 0;

                return OLE_E_ADVISENOTSUPPORTED;
            }
            public int DUnadvise(uint dwConnection) => _ = OLE_E_ADVISENOTSUPPORTED;
            public int EnumDAdvise(IEnumSTATDATA.NativeNotImplemented** ppenumAdvise)
            {
                if(ppenumAdvise is null) return E_POINTER;

                *ppenumAdvise = (IEnumSTATDATA.NativeNotImplemented*)null;

                return OLE_E_ADVISENOTSUPPORTED;
            }
            private unsafe int CreateFileGroupDescriptor(STGMEDIUM* pMedium)
            {
                const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
                const int  fileCount             = 1;

                FILEDESCRIPTORW fileDescriptor;

                fileDescriptor.dwFlags = FD_FLAGS.FD_FILESIZE | FD_FLAGS.FD_ATTRIBUTES /*| FD_FLAGS.FD_PROGRESSUI*/;
                fileDescriptor.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;

                ulong streamLength           = (ulong)_imageStream.Length;
                fileDescriptor.nFileSizeLow = (uint)streamLength;
                fileDescriptor.nFileSizeHigh = (uint)(streamLength >> 32);

                FILEDESCRIPTORW* pDescriptor = &fileDescriptor;
                char* pDestName              = pDescriptor->cFileName;
                int charCountToCopy          = Math.Min(_fileName.Length, MAX_PATH - 1);
                uint byteCountToCopy         = (uint)(charCountToCopy * sizeof(char));

                fixed(char* pSourceName = _fileName) Unsafe.CopyBlock(pDestName, pSourceName, byteCountToCopy);
                pDestName[charCountToCopy] = NULL_TERMINATOR;

                nuint totalSize = (nuint)FILEGROUPDESCRIPTORW.SizeOfUnchecked(fileCount);

                nint hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, totalSize);
                if(hGlobal is 0) return E_OUTOFMEMORY;

                void* pLockedMemory = GlobalLock(hGlobal);
                if(pLockedMemory is null)
                {
                    _ = GlobalFree(hGlobal);
                    return E_FAIL;
                }

                try
                {
                    ref FILEGROUPDESCRIPTORW descriptor = ref Unsafe.AsRef<FILEGROUPDESCRIPTORW>(pLockedMemory);

                    descriptor.cItems = fileCount;
                    descriptor.fgd[0] = fileDescriptor;
                }
                finally { _ = GlobalUnlock(hGlobal); }

                pMedium->tymed = TYMED.TYMED_HGLOBAL;
                pMedium->hGlobal = hGlobal;
                pMedium->pUnkForRelease = (IUnknown.Native*)null;

                return S_OK;
            }
            private unsafe int CreateFileContents(STGMEDIUM* pMedium)
            {
                ReadOnlyMemoryComStream comStream = new ReadOnlyMemoryComStream(_imageStream, _fileName, _localComWrappers);

                nint pUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(comStream, CreateComInterfaceFlags.None);
                if(pUnknown == nint.Zero) return E_FAIL;

                IStream.Native* pStream = null;

                int hr;
                fixed(Guid* pIID = &IID_IStream) hr = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[0])((ComInterfaceDispatch*)pUnknown, pIID, (void**)&pStream);

                _ = Marshal.Release(pUnknown);

                if(hr is not S_OK) return hr;

                pMedium->tymed = TYMED.TYMED_ISTREAM;
                pMedium->pstm = pStream;
                pMedium->pUnkForRelease = (IUnknown.Native*)null;

                return S_OK;
            }
            //public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
            //{
            //    ppv = default;
            //    return CustomQueryInterfaceResult.NotHandled;
            //}
        }
        [GeneratedComClass]
        private unsafe partial class EnumFormatEtc(VirtualFileDragDrop.FORMATETC[] formats, StrategyBasedComWrappers localComWrappers) : IEnumFORMATETC
        {
            private readonly FORMATETC[]              _formats          = formats;
            private readonly StrategyBasedComWrappers _localComWrappers = localComWrappers;
            private int _currentIndex = 0;

            public int Clone(IEnumFORMATETC.Native** ppEnum)
            {
                if(ppEnum is null) return E_POINTER;
                *ppEnum = null;
                EnumFormatEtc cloneInstance = new EnumFormatEtc(_formats,_localComWrappers) { _currentIndex = this._currentIndex };

                nint pUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(cloneInstance, CreateComInterfaceFlags.None);
                if(pUnknown == nint.Zero) return E_FAIL;

                int hr;
                fixed(Guid* pIID = &IID_IEnumFORMATETC)
                {
                    hr =/*QueryInterface*/((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*/*implicitThis*/, Guid*/*riid*/, void**/*ppvObject*/, int/*HRESULT*/>)((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[0])((ComInterfaceDispatch*)pUnknown, pIID, (void**)ppEnum);
                }
                _ = Marshal.Release(pUnknown);
                return hr;
            }
            public int Next(uint celt, FORMATETC* rgelt, uint* pceltFetched)
            {
                if(rgelt is null) return E_POINTER;

                if(pceltFetched is null)
                {
                    if(celt > 1) return E_POINTER;
                }
                else
                {
                    *pceltFetched = 0;
                }

                uint fetchedCount = 0;
                while(fetchedCount < celt && _currentIndex < _formats.Length)
                {
                    rgelt[fetchedCount] = _formats[_currentIndex];

                    fetchedCount++;
                    _currentIndex++;
                }
                if(pceltFetched is not null)
                {
                    *pceltFetched = fetchedCount;
                }
                return (fetchedCount == celt) ? S_OK : S_FALSE;
            }
            public int Reset() { _currentIndex = 0; return S_OK; }
            public int Skip(uint celt)
            {
                long newIndex = _currentIndex + celt;
                if(newIndex >= _formats.Length)
                {
                    _currentIndex = _formats.Length;
                    return S_FALSE;
                }
                else
                {
                    _currentIndex = (int)newIndex;
                    return S_OK;
                }
            }
        }
        [GeneratedComClass]
        private sealed unsafe partial class ReadOnlyMemoryComStream(MemoryStream managedStream, string streamName, StrategyBasedComWrappers localComWrappers) : IStream
        {
            private const int STG_E_ACCESSDENIED                        = unchecked((int)0x80030005);
            private const int STG_E_INVALIDFUNCTION                     = unchecked((int)0x80030001);
            private const int STG_E_READFAULT                           = unchecked((int)0x8003001D);
            private const int STG_E_INVALIDFLAG                         = unchecked((int)0x800300FF);
            private readonly string _streamName                         = streamName;
            private readonly MemoryStream _managedStream                = managedStream;
            private readonly StrategyBasedComWrappers _localComWrappers = localComWrappers;
            // --- ISequentialStream Methods ---
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public unsafe int Read(byte* pv, uint cb, uint* pcbRead) // ~ примерно 2 гигабайта для Span ~ int.MaxValue
            {
                if(pv is null) return E_POINTER;

                switch(cb)
                {
                    case 0:
                    if(pcbRead is not null) *pcbRead = 0;
                    return S_OK;
                    case > int.MaxValue:
                    return E_FAIL;
                }

                Span<byte> buffer = new Span<byte>(pv, (int)cb);

                int totalBytesRead = _managedStream.Read(buffer);

                switch(pcbRead)
                {
                    case not null:
                    *pcbRead = (uint)totalBytesRead;
                    return totalBytesRead < cb ? S_FALSE : S_OK;
                    default:
                    if(totalBytesRead < cb) return STG_E_READFAULT;
                    else return S_OK;
                }
            }
            public int Write(byte* pv, uint cb, uint* pcbWritten) => _ = STG_E_ACCESSDENIED;
            // --- IStream Methods ---
            public int Seek(long dlibMove, uint dwOrigin, ulong* plibNewPosition)
            {
                if(plibNewPosition is null) return E_POINTER;

                if(dwOrigin > (uint)System.IO.SeekOrigin.End) return STG_E_INVALIDFUNCTION;

                long newPosition = _managedStream.Seek(dlibMove, (SeekOrigin)dwOrigin);
                *plibNewPosition = (ulong)newPosition;

                return S_OK;
            }
            public int SetSize(ulong libNewSize) => _ = STG_E_ACCESSDENIED;
            public int CopyTo(IStream.Native* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
            {
                if(pstm is null) return E_POINTER;
                if(pcbRead is not null) *pcbRead = 0;
                if(pcbWritten is not null) *pcbWritten = 0;

                if(_managedStream.TryGetBuffer(out ArraySegment<byte> sourceBuffer) is false)
                {
                    const int STG_E_INVALIDPOINTER = unchecked((int)0x80030009);
                    return STG_E_INVALIDPOINTER;
                }

                long remainingBytes = _managedStream.Length - _managedStream.Position;

                long bytesToCopy = (long)Math.Min(cb, (ulong)remainingBytes);
                if(bytesToCopy <= 0) return S_OK;

                uint bytesWrittenInStep = 0;
                int hr;
                fixed(byte* pSource = MemoryExtensions.AsSpan<byte>(sourceBuffer, (int)_managedStream.Position, (int)bytesToCopy))
                    hr = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, byte*, uint, uint*, int>)(((void**)((ComInterfaceDispatch*)pstm)->Vtable)[4]))((ComInterfaceDispatch*)pstm, pSource, (uint)bytesToCopy, &bytesWrittenInStep);

                if(hr is not S_OK) return hr;

                if(bytesWrittenInStep < bytesToCopy)
                {
                    if(pcbRead is not null) *pcbRead = bytesWrittenInStep;
                    if(pcbWritten is not null) *pcbWritten = bytesWrittenInStep;
                    const int STG_E_WRITEFAULT             = unchecked((int)0x8003001E);
                    return STG_E_WRITEFAULT;
                }

                _managedStream.Position += bytesToCopy;

                if(pcbRead is not null) *pcbRead = (ulong)bytesToCopy;
                if(pcbWritten is not null) *pcbWritten = (ulong)bytesToCopy;

                return S_OK;
            }
            public int Commit(uint grfCommitFlags) => _ = S_OK;
            public int Revert() => _ = S_OK;
            public int LockRegion(ulong libOffset, ulong cb, uint dwLockType) => _ = E_NOTIMPL;
            public int UnlockRegion(ulong libOffset, ulong cb, uint dwLockType) => _ = E_NOTIMPL;
            public int Stat(STATSTG* pstatstg, STATFLAG grfStatFlag)
            {
                if(pstatstg is null) return E_POINTER;

                *pstatstg = default;

                pstatstg->type = STGTY.STGTY_STREAM;
                pstatstg->cbSize = (ulong)_managedStream.Length;

                switch(grfStatFlag)
                {
                    case STATFLAG.STATFLAG_DEFAULT:
                    case STATFLAG.STATFLAG_NOOPEN:
                    nuint sizeInBytes = (nuint)(_streamName.Length + 1) * (nuint)sizeof(char);
                    char* pName = (char*)CoTaskMemAlloc(sizeInBytes);
                    if(pName is null) return E_OUTOFMEMORY;

                    fixed(char* pSourceName = _streamName) Unsafe.CopyBlock(pName, pSourceName, (uint)sizeInBytes - sizeof(char));
                    pName[_streamName.Length] = NULL_TERMINATOR;

                    pstatstg->pwcsName = (nint)pName;
                    return S_OK;
                    case STATFLAG.STATFLAG_NONAME: return S_OK;
                    default: return STG_E_INVALIDFLAG;
                }
            }
            public int Clone(IStream.Native** ppstm)
            {
                if(ppstm is null) return E_POINTER;
                *ppstm = (IStream.Native*)null;

                ReadOnlyMemoryComStream cloneInstance = new ReadOnlyMemoryComStream(_managedStream, _streamName, _localComWrappers);

                cloneInstance._managedStream.Position = this._managedStream.Position;

                nint pUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(cloneInstance, CreateComInterfaceFlags.None);
                if(pUnknown == nint.Zero) return E_FAIL;

                int hr;
                fixed(Guid* pIID = &IID_IStream) hr = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[0])((ComInterfaceDispatch*)pUnknown, pIID, (void**)ppstm);

                _ = Marshal.Release(pUnknown);
                return hr;
            }
        }
        [GeneratedComClass]
        private partial class DropSource : IDropSource /*IDropSourceNotify*/ /*ICustomQueryInterface*/
        {
            private const int  DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
            private const int  DRAGDROP_S_DROP              = 0x00040100;
            private const int  DRAGDROP_S_CANCEL            = 0x00040101;
            private const uint MK_LBUTTON                   = 0x0001;
            private const uint MK_RBUTTON                   = 0x0002;

            public int QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool fEscapePressed, uint grfKeyState)
            {
                if(fEscapePressed is true) return DRAGDROP_S_CANCEL;

                if((grfKeyState & (MK_LBUTTON | MK_RBUTTON)) is 0) return DRAGDROP_S_DROP;

                return S_OK;
            }
            public int GiveFeedback(uint dwEffect) => _ = DRAGDROP_S_USEDEFAULTCURSORS;
            //public int DragEnterTarget(nint hwndTarget) => _ = S_OK;
            //public int DragLeaveTarget() => _ = S_OK;
            //public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
            //{
            //    ppv = default;

            //    return CustomQueryInterfaceResult.NotHandled;
            //}
        }
        [GeneratedComInterface, Guid("00000000-0000-0000-C000-000000000046")]
        public unsafe partial interface IUnknown { public struct Native { } }
        [GeneratedComInterface, Guid("0000010E-0000-0000-C000-000000000046")]
        public unsafe partial interface IDataObject : IUnknown
        {
            [PreserveSig]
            int GetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium);
            [PreserveSig]
            int GetDataHere(FORMATETC* pFormatetc, STGMEDIUM* pMedium);
            [PreserveSig]
            int QueryGetData(FORMATETC* pFormatetc);
            [PreserveSig]
            int GetCanonicalFormatEtc(FORMATETC* pFormatetcIn, FORMATETC* pFormatetcOut);
            [PreserveSig]
            int SetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium, [MarshalAs(UnmanagedType.Bool)] bool fRelease);
            [PreserveSig]
            int EnumFormatEtc(uint dwDirection, IEnumFORMATETC.Native** ppenumFormatEtc);
            [PreserveSig]
            int DAdvise(FORMATETC* pFormatetc, uint advf, IAdviseSink.NativeNotImplemented* pAdvSink, uint* pdwConnection);
            [PreserveSig]
            int DUnadvise(uint dwConnection);
            [PreserveSig]
            int EnumDAdvise(IEnumSTATDATA.NativeNotImplemented** ppenumAdvise);
            new struct Native { }
        }
        [GeneratedComInterface, Guid("0000010F-0000-0000-C000-000000000046")]
        public unsafe partial interface IAdviseSink : IUnknown { public struct NativeNotImplemented { } }
        [GeneratedComInterface, Guid("00000105-0000-0000-C000-000000000046")]
        public unsafe partial interface IEnumSTATDATA : IUnknown { public struct NativeNotImplemented { } }

        [GeneratedComInterface, Guid("00000103-0000-0000-C000-000000000046")]
        public unsafe partial interface IEnumFORMATETC : IUnknown
        {
            [PreserveSig]
            int Next(uint celt, FORMATETC* rgelt, uint* pceltFetched);
            [PreserveSig]
            int Skip(uint celt);
            [PreserveSig]
            int Reset();
            [PreserveSig]
            int Clone(IEnumFORMATETC.Native** ppenum);
            new struct Native { }
        }
        [GeneratedComInterface, Guid("0C733A30-2A1C-11CE-ADE5-00AA0044773D")]
        public unsafe partial interface ISequentialStream : IUnknown
        {
            [PreserveSig]
            int Read(byte* pv, uint cb, uint* pcbRead);
            [PreserveSig]
            int Write(byte* pv, uint cb, uint* pcbWritten);
        }
        [GeneratedComInterface, Guid("0000000C-0000-0000-C000-000000000046")]
        public unsafe partial interface IStream : ISequentialStream
        {
            [PreserveSig]
            int Seek(long dlibMove, uint dwOrigin, ulong* plibNewPosition);
            [PreserveSig]
            int SetSize(ulong libNewSize);
            [PreserveSig]
            int CopyTo(IStream.Native* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten);
            [PreserveSig]
            int Commit(uint grfCommitFlags);
            [PreserveSig]
            int Revert();
            [PreserveSig]
            int LockRegion(ulong libOffset, ulong cb, uint dwLockType);
            [PreserveSig]
            int UnlockRegion(ulong libOffset, ulong cb, uint dwLockType);
            [PreserveSig]
            int Stat(STATSTG* pstatstg, STATFLAG grfStatFlag);
            [PreserveSig]
            int Clone(IStream.Native** ppstm);
            new struct Native { }
        }
        [GeneratedComInterface, Guid("00000121-0000-0000-C000-000000000046")]
        public unsafe partial interface IDropSource : IUnknown
        {
            [PreserveSig]
            int QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool fEscapePressed, uint grfKeyState);
            [PreserveSig]
            int GiveFeedback(uint dwEffect);
            new struct Native { }
        }
        [GeneratedComInterface, Guid("0000012b-0000-0000-C000-000000000046")]
        public unsafe partial interface IDropSourceNotify : IUnknown
        {
            [PreserveSig]
            int DragEnterTarget(nint hwndTarget);
            [PreserveSig]
            int DragLeaveTarget();
            new struct Native { }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public int x;
            public int y;
        }
        [Flags]
        public enum FD_FLAGS : uint
        {
            FD_CLSID      = 0x00000001,
            FD_SIZEPOINT  = 0x00000002,
            FD_ATTRIBUTES = 0x00000004,
            FD_CREATETIME = 0x00000008,
            FD_ACCESSTIME = 0x00000010,
            FD_WRITESTIME = 0x00000020,
            FD_FILESIZE   = 0x00000040,
            FD_PROGRESSUI = 0x00004000,
            FD_LINKUI     = 0x00008000
        }
        public struct VariableLengthInlineArray<T> where T : unmanaged
        {
            public T ElementIndexZero;
            public ref T this[int index]
            {
                [UnscopedRef]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Add(ref this.ElementIndexZero, index);
            }
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan(int length) => MemoryMarshal.CreateSpan(ref this.ElementIndexZero, length);
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FILEGROUPDESCRIPTORW
        {
            public uint cItems;
            public VariableLengthInlineArray<FILEDESCRIPTORW> fgd;
            public static int SizeOfUnchecked(int count) => sizeof(FILEGROUPDESCRIPTORW) + (count - 1) * sizeof(FILEDESCRIPTORW);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZEL
        {
            public int cx;
            public int cy;
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FILEDESCRIPTORW
        {
            public FD_FLAGS dwFlags;

            public Guid     clsid;
            public SIZEL    sizel;
            public POINTL   pointl;

            public uint     dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint     nFileSizeHigh;
            public uint     nFileSizeLow;

            public fixed char cFileName[VirtualFileDragDrop.MAX_PATH];
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FORMATETC
        {
            public ushort          cfFormat;
            public DVTARGETDEVICE* ptd;
            public DVASPECT        dwAspect;
            public int             lindex;
            public TYMED           tymed;
        }
        public enum DVASPECT : uint
        {
            DVASPECT_CONTENT     = 1,
            DVASPECT_THUMBNAIL   = 2,
            DVASPECT_ICON        = 4,
            DVASPECT_DOCPRINT    = 8,
            DVASPECT_OPAQUE      = 16,
            DVASPECT_TRANSPARENT = 32
        }
        [Flags]
        public enum TYMED
        {
            /// <summary>The storage medium is a global memory handle (<b>HGLOBAL</b>). Allocate the global handle with the GMEM_MOVEABLE flag. If the <b>punkForRelease</b> member of <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> is <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/winbase/nf-winbase-globalfree">GlobalFree</a> to release the memory.</summary>
            TYMED_HGLOBAL  = 1,
            /// <summary>The storage medium is a disk file identified by a path. If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/winbase/nf-winbase-openfile">OpenFile</a> to delete the file.</summary>
            TYMED_FILE     = 2,
            /// <summary>The storage medium is a stream object identified by an <a href="https://docs.microsoft.com/windows/desktop/api/objidl/nn-objidl-istream">IStream</a> pointer. Use <a href="https://docs.microsoft.com/windows/desktop/api/objidl/nf-objidl-isequentialstream-read">ISequentialStream::Read</a> to read the data. If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is not <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/unknwn/nf-unknwn-iunknown-release">Release</a> to release the stream component.</summary>
            TYMED_ISTREAM  = 4,
            /// <summary>The storage medium is a storage component identified by an <a href="https://docs.microsoft.com/windows/desktop/api/objidl/nn-objidl-istorage">IStorage</a> pointer. The data is in the streams and storages contained by this <b>IStorage</b> instance. If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is not <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/unknwn/nf-unknwn-iunknown-release">Release</a> to release the storage component.</summary>
            TYMED_ISTORAGE = 8,
            /// <summary>The storage medium is a GDI component (<b>HBITMAP</b>). If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/wingdi/nf-wingdi-deleteobject">DeleteObject</a> to delete the bitmap.</summary>
            TYMED_GDI      = 16,
            /// <summary>The storage medium is a metafile (<b>METAFILEPICT</b>). Use the GDI functions to access the metafile's data. If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/wingdi/nf-wingdi-deletemetafile">DeleteMetaFile</a> to delete the bitmap.</summary>
            TYMED_MFPICT   = 32,
            /// <summary>The storage medium is an enhanced metafile (<b>HENHMETAFILE</b>). If the <a href="https://docs.microsoft.com/windows/win32/api/objidl/ns-objidl-ustgmedium-r1">STGMEDIUM</a> <b>punkForRelease</b> member is <b>NULL</b>, the destination process should use <a href="https://docs.microsoft.com/windows/desktop/api/wingdi/nf-wingdi-deleteenhmetafile">DeleteEnhMetaFile</a> to delete the bitmap.</summary>
            TYMED_ENHMF    = 64,
            /// <summary>No data is being passed.</summary>
            TYMED_NULL      = 0,
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DVTARGETDEVICE
        {
            public uint tdSize;
            public ushort tdDriverNameOffset;
            public ushort tdDeviceNameOffset;
            public ushort tdPortNameOffset;
            public ushort tdExtDevmodeOffset;
        }
        public enum STGTY : int
        {
            STGTY_STORAGE   = 1,
            STGTY_STREAM    = 2,
            STGTY_LOCKBYTES = 3,
            STGTY_PROPERTY  = 4
        }
        public enum STATFLAG : uint
        {
            STATFLAG_DEFAULT = 0,
            STATFLAG_NONAME  = 1,
            STATFLAG_NOOPEN  = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct STATSTG
        {
            public nint     pwcsName;
            public STGTY    type;
            public ulong    cbSize;
            public FILETIME mtime;
            public FILETIME ctime;
            public FILETIME atime;
            public uint     grfMode;
            public uint     grfLocksSupported;
            public Guid     clsid;
            public uint     grfStateBits;
            public uint     reserved;
        }
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct STGMEDIUM
        {
            [FieldOffset(0)]
            public TYMED tymed;

            [FieldOffset(8)]
            public nint hGlobal;

            [FieldOffset(8)]
            public IStream.Native* pstm;

            [FieldOffset(16)]
            public IUnknown.Native* pUnkForRelease;
        }
        [Flags]
        private enum DROPEFFECT : uint
        {
            DROPEFFECT_NONE   = 0,
            DROPEFFECT_COPY   = 1,
            DROPEFFECT_MOVE   = 2,
            DROPEFFECT_LINK   = 4,
            DROPEFFECT_SCROLL = 0x80000000,
        }
        [LibraryImport("user32", SetLastError = true)]
        private static partial uint RegisterClipboardFormatW([MarshalAs(UnmanagedType.LPWStr)] string lpszFormat);
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        [LibraryImport("kernel32", SetLastError = true)]
        private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);
        [LibraryImport("kernel32", SetLastError = true)]
        private static partial nint GlobalFree(nint hMem);
        [LibraryImport("kernel32", SetLastError = true)]
        private static unsafe partial void* GlobalLock(nint hMem);
        [LibraryImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalUnlock(nint hMem);
        [LibraryImport("ole32", SetLastError = true)]
        private static unsafe partial void* CoTaskMemAlloc(nuint cb);
        [LibraryImport("ole32", SetLastError = true)]
        private static unsafe partial int DoDragDrop(IDataObject.Native* pDataObj, IDropSource.Native* pDropSource, uint dwOKEffects, uint* pdwEffect);
    }
}


