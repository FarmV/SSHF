using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using static System.Runtime.InteropServices.ComWrappers;

namespace FVH.SSHF.Infrastructure
{
    internal sealed unsafe partial class VirtualFileDragDrop
    {
        private const int  S_OK            = 0x00000000;
        private const int  S_FALSE         = 0x1;
        private const int  E_NOTIMPL       = unchecked((int)0x80004001);
        private const int  E_POINTER       = unchecked((int)0x80004003);
        private const int  E_FAIL          = unchecked((int)0x80004005);
        private const int  DV_E_FORMATETC  = unchecked((int)0x80040064);
        private const int  E_INVALIDARG    = unchecked((int)0x80070057);
        private const int  MAX_PATH        = 260;
        private const char NULL_TERMINATOR = '\0';
        // Base
        private static readonly ushort s_fileGroupDescriptorFormatId        = (ushort)RegisterClipboardFormatW("FileGroupDescriptorW");
        private static readonly ushort s_fileContentsFormatId               = (ushort)RegisterClipboardFormatW("FileContents");
        // Extension
        private static readonly ushort s_pngFormatId                        = (ushort)RegisterClipboardFormatW("PNG");
        private static readonly ushort s_preferredDropEffectFormatId        = (ushort)RegisterClipboardFormatW("Preferred DropEffect");
        // Dynamic IDragSourceHelper
        private static readonly ushort s_dragImageBitsFormatId              = (ushort)RegisterClipboardFormatW("DragImageBits");
        private static readonly ushort s_dragContextFormatId                = (ushort)RegisterClipboardFormatW("DragContext");
        // Dynamic DoDragDrop
        private static readonly ushort s_isShowingLayeredFormatId           = (ushort)RegisterClipboardFormatW("IsShowingLayered");
        private static readonly ushort s_dragWindowFormatId                 = (ushort)RegisterClipboardFormatW("DragWindow");
        private static readonly ushort s_dropDescriptionFormatId            = (ushort)RegisterClipboardFormatW("DropDescription");
        private static readonly ushort s_disableDragTextFormatId            = (ushort)RegisterClipboardFormatW("DisableDragText");
        private static readonly ushort s_isShowingTextFormatId              = (ushort)RegisterClipboardFormatW("IsShowingText");
        private static readonly ushort s_targetClsidFormatId                = (ushort)RegisterClipboardFormatW("TargetCLSID");
        private static readonly ushort s_performedDropEffectFormatId        = (ushort)RegisterClipboardFormatW("Performed DropEffect");
        private static readonly ushort s_logicalPerformedDropEffectFormatId = (ushort)RegisterClipboardFormatW("Logical Performed DropEffect");
        // Filter supported
        private static readonly ushort s_shellIdListArrayFormatId           = (ushort)RegisterClipboardFormatW("Shell IDList Array");

        private static readonly Guid IID_IEnumFORMATETC     = typeof(IEnumFORMATETC)    .GUID;
        private static readonly Guid IID_IDataObject        = typeof(IDataObject)       .GUID;
        private static readonly Guid IID_IDropSource        = typeof(IDropSource)       .GUID;
        private static readonly Guid IID_IStream            = typeof(IStream)           .GUID;
        private static readonly Guid IID_IDragSourceHelper2 = typeof(IDragSourceHelper2).GUID;
        private static readonly Guid IID_IDragSourceHelper  = typeof(IDragSourceHelper) .GUID;
        private static readonly Guid CLSID_DragDropHelper   = new Guid("4657278A-411B-11d2-839A-00C04FD918D0");

        private static readonly StrategyBasedComWrappers s_localComWrappers = new StrategyBasedComWrappers();

        public VirtualFileDragDrop() { }
                     
        public unsafe void InitiateDrop(MemoryStream imageStream, string fileName, DragDropOptions options)
        {
            const int IUnknown_QueryInterface_VTableIndex = 0;

            nint pUnkDataObject = nint.Zero;
            nint pUnkDropSource = nint.Zero;
            IDragSourceHelper2.Native* pDragSourceHelper2 = null;
            IDataObject.Native* pDataObj  = null;
            IDropSource.Native* pDropSrc  = null;
         
            DataObject? dataObject = null;

            try
            {
                dataObject = new DataObject(imageStream, fileName, s_localComWrappers);
                DropSource dropSource = new DropSource();

                pUnkDataObject = s_localComWrappers.GetOrCreateComInterfaceForObject(dataObject, CreateComInterfaceFlags.None);
                if(pUnkDataObject == nint.Zero) ThrowArgumentNull(nameof(dataObject));

                pUnkDropSource = s_localComWrappers.GetOrCreateComInterfaceForObject(dropSource, CreateComInterfaceFlags.None);
                if(pUnkDropSource == nint.Zero) ThrowArgumentNull(nameof(dropSource));
             
                int hResultQI_DataObject;
                fixed(Guid* pIID = &IID_IDataObject) hResultQI_DataObject = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)(((void**)((ComInterfaceDispatch*)pUnkDataObject)->Vtable)[IUnknown_QueryInterface_VTableIndex])) ((ComInterfaceDispatch*)pUnkDataObject, pIID, (void**)&pDataObj);
                if(hResultQI_DataObject < S_OK) ThrowQueryInterface(nameof(IDataObject), hResultQI_DataObject);

                int hResultQI_DropSource;
                fixed(Guid* pIID = &IID_IDropSource) hResultQI_DropSource = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)(((void**)((ComInterfaceDispatch*)pUnkDropSource)->Vtable)[IUnknown_QueryInterface_VTableIndex]))((ComInterfaceDispatch*)pUnkDropSource, pIID, (void**)&pDropSrc);
                if(hResultQI_DropSource < S_OK) ThrowQueryInterface(nameof(IDropSource), hResultQI_DropSource);

                if(options.HBitmap != nint.Zero)
                {
                    int hrCreate;
                    const nint NoAggregation = 0;
                    fixed(Guid* pClsid = &CLSID_DragDropHelper, pIid = &IID_IDragSourceHelper2) hrCreate = CoCreateInstance(pClsid, (IUnknown.Native*)NoAggregation, CLSCTX_INPROC_SERVER, pIid, (IUnknown.Native**)&pDragSourceHelper2);
                    if(hrCreate < S_OK) ThrowCoCreateInstance("DragDropHelper", hrCreate);

                    SIZE bitmapSize = GetBitmapSize(options.HBitmap);
                    SHDRAGIMAGE dragImageInfo = new()
                    {
                        sizeDragImage  = bitmapSize,
                        hbmpDragImage  = options.HBitmap,
                        ptOffset       = new POINT { x = bitmapSize.cx / 2, y = bitmapSize.cy / 2 },
                        crColorKey     = 0xffffffffu // CLR_NONE
                    };
         
                    const int IDragSourceHelper_InitializeFromBitmap_VTableIndex = 3;
                    int hrInitializeFromBitmap = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, SHDRAGIMAGE*, IDataObject.Native*, int>)(((void**)((ComInterfaceDispatch*)pDragSourceHelper2)->Vtable)[IDragSourceHelper_InitializeFromBitmap_VTableIndex]))((ComInterfaceDispatch*)pDragSourceHelper2, &dragImageInfo, pDataObj);
          
                    if(hrInitializeFromBitmap < S_OK) ThrowInitializeFromBitmap(hrInitializeFromBitmap);
                }
            
                const uint allowedEffects = (uint)DROPEFFECT.DROPEFFECT_COPY;
                uint performedEffect = 0;
                int hrDoDragDrop = DoDragDrop(pDataObj, pDropSrc, allowedEffects, &performedEffect);

                if(hrDoDragDrop < S_OK)
                {
#if DEBUG
                    if(Debugger.IsAttached)
                    {
                        if(hrDoDragDrop == E_POINTER) Debugger.Break();
                        if(hrDoDragDrop == E_INVALIDARG) Debugger.Break();
                        Debugger.Break();
                    }
#endif
                    ThrowDoDragDrop(hrDoDragDrop);
                }   
            }
            finally
            {
                if(pDragSourceHelper2 is not null) _ = Marshal.Release((nint)pDragSourceHelper2);
                if(pDropSrc is not null)           _ = Marshal.Release((nint)pDropSrc);
                if(pDataObj is not null)           _ = Marshal.Release((nint)pDataObj);
                if(pUnkDropSource != nint.Zero)    _ = Marshal.Release(pUnkDropSource);
                if(pUnkDataObject != nint.Zero)    _ = Marshal.Release(pUnkDataObject);

                dataObject?.Dispose();
            }
            [DoesNotReturn] static void ThrowArgumentNull(string pointerName)                  => throw new ArgumentNullException(pointerName, $"Failed to create COM interface pointer for {pointerName}.");
            [DoesNotReturn] static void ThrowQueryInterface(string interfaceName, int hResult) => throw new COMException($"QueryInterface failed for interface '{interfaceName}' with HRESULT: 0x{hResult:X8}.", hResult);
            [DoesNotReturn] static void ThrowDoDragDrop(int hResult)                           => throw new COMException($"DoDragDrop function failed with HRESULT: 0x{hResult:X8}.", hResult);
            [DoesNotReturn] static void ThrowCoCreateInstance(string className, int hResult)   => throw new COMException($"CoCreateInstance failed for '{className}' with HRESULT: 0x{hResult:X8}.", hResult);
            [DoesNotReturn] static void ThrowInitializeFromBitmap(int hResult)                 => throw new COMException($"IDragSourceHelper::InitializeFromBitmap failed with HRESULT: 0x{hResult:X8}.", hResult);
            static unsafe SIZE GetBitmapSize(nint hBitmap)
            {
                BITMAP bmp = default;

                int bytesWritten = GetObjectW(hBitmap, sizeof(BITMAP), &bmp);

                if(bytesWritten is 0) ThrowGetObjectFailed(); [DoesNotReturn] static void ThrowGetObjectFailed() => throw new InvalidOperationException("Win32 function 'GetObjectW' failed to retrieve bitmap information.");

                return new SIZE { cx = bmp.bmWidth, cy = bmp.bmHeight };
            }
        }
        public static partial class Helper
        {
            public static unsafe nint CreateHBitmapFromBitmapSource(BitmapSource bitmapSource)
            {
                FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);
                int width = convertedBitmap.PixelWidth;
                int height = convertedBitmap.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                convertedBitmap.CopyPixels(pixels, stride, 0);

                BITMAPINFOHEADER bmiHeader = new BITMAPINFOHEADER()
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = width,
                    biHeight = -height, // <-- ВАЖНО: top-down DIB, первая строка - верхняя
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0 // BI_RGB
                };

                nint pBits = nint.Zero;
                const uint DIB_RGB_COLORS = 0; 
                nint hBitmap = CreateDIBSection(nint.Zero, &bmiHeader, DIB_RGB_COLORS,&pBits, nint.Zero, 0);

                if(hBitmap == nint.Zero) return nint.Zero;

                fixed(byte* pPixels = pixels) Unsafe.CopyBlock((void*)pBits, pPixels, (uint)pixels.Length);

                return hBitmap;
            }
            [LibraryImport("gdi32")]
            private static unsafe partial nint CreateDIBSection(nint hdc, BITMAPINFOHEADER* pbmi, uint usage, nint* ppvBits, nint hSection, uint offset);

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFOHEADER
            {
                public uint   biSize;
                public int    biWidth;
                public int    biHeight;
                public ushort biPlanes;
                public ushort biBitCount;
                public uint   biCompression;
                public uint   biSizeImage;
                public int    biXPelsPerMeter;
                public int    biYPelsPerMeter;
                public uint   biClrUsed;
                public uint   biClrImportant;
            }        
        }
        [GeneratedComClass]
        private unsafe partial class DataObject : IDataObject, IDisposable /*ICustomQueryInterface*/
        {
            private const    int                       OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
            private const    int                       DV_E_TYMED               = unchecked((int)0x80040069);
            private readonly string                   _fileName;
            private readonly StrategyBasedComWrappers _localComWrappers;
            private readonly ReadOnlyMemoryComStream  _comStream;

            private bool _isDisposed = false;

            private STGMEDIUM? _dragImageBits;
            private STGMEDIUM? _dragContext;

            private STGMEDIUM? _isShowingLayered;
            private STGMEDIUM? _dragWindow;
            private STGMEDIUM? _dropDescription;
            private STGMEDIUM? _disableDragText;
            private STGMEDIUM? _isShowingText;

            private STGMEDIUM? _targetClsid;
            private STGMEDIUM? _performedDropEffect;
            private STGMEDIUM? _logicalPerformedDropEffect;

            public DataObject(MemoryStream imageStream, string fileName, StrategyBasedComWrappers localComWrappers)
            {
                _localComWrappers = localComWrappers;
                _fileName         = fileName;

                _comStream        = new ReadOnlyMemoryComStream(imageStream, fileName, localComWrappers);
            }

            public void Dispose()
            {
                if(_isDisposed) return;
                _isDisposed = true;

                ReleaseMediumIfAvailable(ref _dragImageBits);
                ReleaseMediumIfAvailable(ref _dragContext);
                ReleaseMediumIfAvailable(ref _isShowingLayered);
                ReleaseMediumIfAvailable(ref _dragWindow);
                ReleaseMediumIfAvailable(ref _dropDescription);
                ReleaseMediumIfAvailable(ref _disableDragText);
                ReleaseMediumIfAvailable(ref _isShowingText);
                ReleaseMediumIfAvailable(ref _targetClsid);
                ReleaseMediumIfAvailable(ref _performedDropEffect);
                ReleaseMediumIfAvailable(ref _logicalPerformedDropEffect);

                static void ReleaseMediumIfAvailable(ref STGMEDIUM? medium)
                {
                    if(medium.HasValue)
                    {
                        STGMEDIUM toRelease = medium.Value;
                        ReleaseStgMedium(&toRelease);
                        medium = null;
                    }
                }
            }

            public int EnumFormatEtc(uint dwDirection, IEnumFORMATETC.Native** ppenumFormatEtc)
            {
                if(ppenumFormatEtc is null) return E_POINTER;
                *ppenumFormatEtc = (IEnumFORMATETC.Native*)null;

                const uint DATADIR_GET = 1;
                if(dwDirection is not DATADIR_GET) return E_NOTIMPL;

                Span<FORMATETC> supportedFormatsSpan = stackalloc FORMATETC[14];
                int formatCount = 0;

                supportedFormatsSpan[formatCount++] = new FORMATETC()
                {
                    cfFormat = s_pngFormatId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex   = -1,
                    tymed    = TYMED.TYMED_HGLOBAL
                };
                supportedFormatsSpan[formatCount++] = new FORMATETC()
                {
                    cfFormat = s_preferredDropEffectFormatId, 
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    tymed = TYMED.TYMED_HGLOBAL
                };
                supportedFormatsSpan[formatCount++] = new FORMATETC()
                {
                    cfFormat = s_fileGroupDescriptorFormatId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex   = -1,
                    tymed    = TYMED.TYMED_HGLOBAL
                };
                supportedFormatsSpan[formatCount++] = new FORMATETC()
                {
                    cfFormat = s_fileContentsFormatId,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex   = 0,
                    tymed = TYMED.TYMED_ISTREAM
                };

                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_dragImageBitsFormatId,              _dragImageBits);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_dragContextFormatId,                _dragContext);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_isShowingLayeredFormatId,           _isShowingLayered);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_dragWindowFormatId,                 _dragWindow);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_dropDescriptionFormatId,            _dropDescription);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_disableDragTextFormatId,            _disableDragText);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_isShowingTextFormatId,              _isShowingText);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_targetClsidFormatId,                _targetClsid);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_performedDropEffectFormatId,        _performedDropEffect);
                AddFormatIfAvailable(ref formatCount, supportedFormatsSpan, s_logicalPerformedDropEffectFormatId, _logicalPerformedDropEffect);

                FORMATETC[] finalFormats = supportedFormatsSpan.Slice(0, formatCount).ToArray();

                EnumFormatEtc enumerator = new EnumFormatEtc(finalFormats, _localComWrappers);

                nint pointerIUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(enumerator, CreateComInterfaceFlags.None);
                if(pointerIUnknown == nint.Zero) return E_FAIL;

                const int IUnknown_QueryInterface_VTableIndex = 0;
                int hResultQI_EnumFormatEtc;
                fixed(Guid* pIID = &IID_IEnumFORMATETC) hResultQI_EnumFormatEtc = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)((void**)((ComInterfaceDispatch*)pointerIUnknown)->Vtable)[IUnknown_QueryInterface_VTableIndex])((ComInterfaceDispatch*)pointerIUnknown, pIID, (void**)ppenumFormatEtc);

                _ = Marshal.Release(pointerIUnknown);

                return hResultQI_EnumFormatEtc;

                static void AddFormatIfAvailable(ref int formatCount, Span<FORMATETC> formats, ushort formatId, STGMEDIUM? medium)
                {
                    if(medium.HasValue is false) return;

                    formats[formatCount++] = new FORMATETC()
                    {
                        cfFormat = formatId,
                        dwAspect = DVASPECT.DVASPECT_CONTENT,
                        lindex   = -1,
                        tymed    = medium.Value.tymed
                    };
                }
            }
            public int GetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium)
            {
                if(pMedium is null) return E_POINTER;
                *pMedium = default;

                switch(pFormatetc->cfFormat)
                {   // Extension
                    case ushort formatId when formatId == s_preferredDropEffectFormatId: return CreatePreferredDropEffect(pMedium);
                    case ushort formatId when formatId == s_pngFormatId:                 return CreatePngData(pMedium);
                    // Base
                    case ushort formatId when formatId == s_fileGroupDescriptorFormatId: return CreateFileGroupDescriptor(pMedium);
                    case ushort formatId when formatId == s_fileContentsFormatId:        return CreateFileContents(pMedium);
                    // Dynamic
                    case ushort formatId when formatId == s_dragImageBitsFormatId && _dragImageBits.HasValue:                           return CopyCachedStgMedium(_dragImageBits.Value, pMedium);
                    case ushort formatId when formatId == s_dragContextFormatId && _dragContext.HasValue:                               return CopyCachedStgMedium(_dragContext.Value, pMedium);
                    case ushort formatId when formatId == s_isShowingLayeredFormatId && _isShowingLayered.HasValue:                     return CopyCachedStgMedium(_isShowingLayered.Value, pMedium);
                    case ushort formatId when formatId == s_dragWindowFormatId && _dragWindow.HasValue:                                 return CopyCachedStgMedium(_dragWindow.Value, pMedium);
                    case ushort formatId when formatId == s_dropDescriptionFormatId && _dropDescription.HasValue:                       return CopyCachedStgMedium(_dropDescription.Value, pMedium);
                    case ushort formatId when formatId == s_disableDragTextFormatId && _disableDragText.HasValue:                       return CopyCachedStgMedium(_disableDragText.Value, pMedium);
                    case ushort formatId when formatId == s_isShowingTextFormatId && _isShowingText.HasValue:                           return CopyCachedStgMedium(_isShowingText.Value, pMedium);
                    case ushort formatId when formatId == s_targetClsidFormatId && _targetClsid.HasValue:                               return CopyCachedStgMedium(_targetClsid.Value, pMedium);
                    case ushort formatId when formatId == s_performedDropEffectFormatId && _performedDropEffect.HasValue:               return CopyCachedStgMedium(_performedDropEffect.Value, pMedium);
                    case ushort formatId when formatId == s_logicalPerformedDropEffectFormatId && _logicalPerformedDropEffect.HasValue: return CopyCachedStgMedium(_logicalPerformedDropEffect.Value, pMedium);

                    default: return DV_E_FORMATETC;
                }
            }
            private unsafe int CopyCachedStgMedium(in STGMEDIUM source, STGMEDIUM* pDestination)
            {
                STGMEDIUM destinationMedium = default;

                switch(source.tymed)
                {
                    case TYMED.TYMED_HGLOBAL:
                        const int default_GMEM_MOVEABLE = 0;
                        nint hDuplicated = OleDuplicateData(source.hGlobal, 0, default_GMEM_MOVEABLE);
                        if(hDuplicated == nint.Zero) return E_OUTOFMEMORY;
                        
                        destinationMedium.tymed          = TYMED.TYMED_HGLOBAL;
                        destinationMedium.hGlobal        = hDuplicated;
                        destinationMedium.pUnkForRelease = null;
                    break;

                    case TYMED.TYMED_ISTREAM:
                        IStream.Native* pSourceStream = source.pstm;
                        if(pSourceStream is null) return E_POINTER;
                        
                        IStream.Native* pClonedStream = null;
                        
                        const int IStream_Clone_VTableIndex = 13; // IStream::Clone находится на 14-й позиции в VTable (индекс 13)
                        int hrClone = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, IStream.Native**, int>)(((void**)((ComInterfaceDispatch*)pSourceStream)->Vtable)[IStream_Clone_VTableIndex]))((ComInterfaceDispatch*)pSourceStream, &pClonedStream);

                        if(hrClone is not S_OK) return hrClone;

                        destinationMedium.tymed          = TYMED.TYMED_ISTREAM;
                        destinationMedium.pstm           = pClonedStream;
                        destinationMedium.pUnkForRelease = (IUnknown.Native*)pClonedStream;
                    break;

                    default: return DV_E_TYMED;
                }

                *pDestination = destinationMedium;
                return S_OK;
            }    
            public int QueryGetData(FORMATETC* pFormatetc)
            {
                const ushort CF_HDROP = 15;
                switch(pFormatetc->cfFormat)
                {   // Expected unsupported format
                    case ushort formatId when formatId == CF_HDROP:                   return DV_E_FORMATETC;
                    case ushort formatId when formatId == s_shellIdListArrayFormatId: return DV_E_FORMATETC;
                    // Extension
                    case ushort formatId when formatId == s_pngFormatId:                                                                return S_OK;
                    case ushort formatId when formatId == s_preferredDropEffectFormatId:                                                return S_OK;
                    // Base
                    case ushort formatId when formatId == s_fileGroupDescriptorFormatId || formatId == s_fileContentsFormatId:          return S_OK;
                    // Dynamic
                    case ushort formatId when formatId == s_dragImageBitsFormatId              && _dragImageBits.HasValue:              return S_OK;
                    case ushort formatId when formatId == s_dragContextFormatId                && _dragContext.HasValue:                return S_OK;
                    case ushort formatId when formatId == s_isShowingLayeredFormatId           && _isShowingLayered.HasValue:           return S_OK;
                    case ushort formatId when formatId == s_dragWindowFormatId                 && _dragWindow.HasValue:                 return S_OK;
                    case ushort formatId when formatId == s_dropDescriptionFormatId            && _dropDescription.HasValue:            return S_OK;
                    case ushort formatId when formatId == s_disableDragTextFormatId            && _disableDragText.HasValue:            return S_OK;
                    case ushort formatId when formatId == s_isShowingTextFormatId              && _isShowingText.HasValue:              return S_OK;
                    case ushort formatId when formatId == s_targetClsidFormatId                && _targetClsid.HasValue:                return S_OK;
                    case ushort formatId when formatId == s_performedDropEffectFormatId        && _performedDropEffect.HasValue:        return S_OK;
                    case ushort formatId when formatId == s_logicalPerformedDropEffectFormatId && _logicalPerformedDropEffect.HasValue: return S_OK;

                    default:
#if DEBUG
                        LogUnsupportedFormat(pFormatetc);
#endif
                    return DV_E_FORMATETC;
                }
            }
            public int GetDataHere(FORMATETC* pFormatetc, STGMEDIUM* pMedium) => S_OK;
            public int GetCanonicalFormatEtc(FORMATETC* pFormatetcIn, FORMATETC* pFormatetcOut)
            {
                if(pFormatetcOut is null) return E_POINTER;
                *pFormatetcOut = default;

                const int DATA_E_FORMATETC = unchecked((int)0x80040064);
                return DATA_E_FORMATETC;
            }          
            public int SetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium, [MarshalAs(UnmanagedType.Bool)] bool fRelease)
            {
                return pFormatetc->cfFormat switch
                {
                    ushort id when id == s_dragImageBitsFormatId              => HandleSetData(pFormatetc, pMedium, fRelease, ref _dragImageBits),
                    ushort id when id == s_dragContextFormatId                => HandleSetData(pFormatetc, pMedium, fRelease, ref _dragContext),
                    ushort id when id == s_isShowingLayeredFormatId           => HandleSetData(pFormatetc, pMedium, fRelease, ref _isShowingLayered),
                    ushort id when id == s_dragWindowFormatId                 => HandleSetData(pFormatetc, pMedium, fRelease, ref _dragWindow),
                    ushort id when id == s_dropDescriptionFormatId            => HandleSetData(pFormatetc, pMedium, fRelease, ref _dropDescription),
                    ushort id when id == s_disableDragTextFormatId            => HandleSetData(pFormatetc, pMedium, fRelease, ref _disableDragText),
                    ushort id when id == s_isShowingTextFormatId              => HandleSetData(pFormatetc, pMedium, fRelease, ref _isShowingText),
                    ushort id when id == s_targetClsidFormatId                => HandleSetData(pFormatetc, pMedium, fRelease, ref _targetClsid),
                    ushort id when id == s_performedDropEffectFormatId        => HandleSetData(pFormatetc, pMedium, fRelease, ref _performedDropEffect),
                    ushort id when id == s_logicalPerformedDropEffectFormatId => HandleSetData(pFormatetc, pMedium, fRelease, ref _logicalPerformedDropEffect),
                    _ => GetNOTIMPL(pFormatetc)
                };
                static int GetNOTIMPL(FORMATETC* pFormatetc)
                {
#if DEBUG                  
                    LogUnsupportedFormat(pFormatetc);
#endif
                    return E_NOTIMPL;
                }
            }         
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

                fileDescriptor.dwFlags          = FD_FLAGS.FD_FILESIZE | FD_FLAGS.FD_ATTRIBUTES;
                fileDescriptor.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;

                STATSTG statstg;
                _ = _comStream.Stat(&statstg, STATFLAG.STATFLAG_DEFAULT);

                ulong streamLength = statstg.cbSize;
             
                CoTaskMemFree((void*)statstg.pwcsName);

                fileDescriptor.nFileSizeLow  = (uint)streamLength;
                fileDescriptor.nFileSizeHigh = (uint)(streamLength >> 32);


                FILEDESCRIPTORW* pDescriptor = &fileDescriptor;
                char* pDestName              = pDescriptor->cFileName;
                int charCountToCopy          = Math.Min(_fileName.Length, MAX_PATH - 1);

                _fileName.CopyTo(new Span<char>(pDestName, charCountToCopy));
                pDestName[charCountToCopy] = NULL_TERMINATOR;

                nuint totalSize = (nuint)FILEGROUPDESCRIPTORW.SizeOfUnchecked(fileCount);
                nint hGlobal    = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, totalSize);
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

                pMedium->tymed          = TYMED.TYMED_HGLOBAL;
                pMedium->hGlobal        = hGlobal;
                pMedium->pUnkForRelease = (IUnknown.Native*)null;

                return S_OK;
            }
            private unsafe int CreateFileContents(STGMEDIUM* pMedium)
            {
                nint pUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(_comStream, CreateComInterfaceFlags.None);
                if(pUnknown == nint.Zero) return E_FAIL;

                IStream.Native* pStream       = null;
                IStream.Native* pClonedStream = null;
                int hResult;

                try
                {
                    const int IUnknown_QueryInterface_VTableIndex = 0;
                    int hResultQI_IStream;
                    fixed(Guid* pIID = &IID_IStream) hResultQI_IStream = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)(((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[IUnknown_QueryInterface_VTableIndex]))((ComInterfaceDispatch*)pUnknown, pIID, (void**)&pStream);
                    
                    if(hResultQI_IStream is not S_OK)
                    {
                        hResult = hResultQI_IStream;
                        return hResult;
                    }

                    const int IStream_Clone_VTableIndex = 13;
                    int hResultClone = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, IStream.Native**, int>)(((void**)((ComInterfaceDispatch*)pStream)->Vtable)[IStream_Clone_VTableIndex]))((ComInterfaceDispatch*)pStream, &pClonedStream);

                    if(hResultClone is not S_OK)
                    {
                        if(pClonedStream is not null) _ = Marshal.Release((nint)pClonedStream);
                        hResult = hResultClone;
                        return hResult;
                    }

                    pMedium->tymed = TYMED.TYMED_ISTREAM;
                    pMedium->pstm = pClonedStream;
                    pMedium->pUnkForRelease = (IUnknown.Native*)pClonedStream;

                    pClonedStream = null;
                }
                finally
                {
                    if(pClonedStream is not null) _ = Marshal.Release((nint)pClonedStream);
                    if(pStream is not null)       _ = Marshal.Release((nint)pStream);
                    _ = Marshal.Release(pUnknown);
                }

                return S_OK;
            }
            private unsafe int CreatePngData(STGMEDIUM* pMedium)
            {
                STATSTG statstg;
                _ = _comStream.Stat(&statstg, STATFLAG.STATFLAG_NONAME);
                ulong streamLength = statstg.cbSize;

                nint hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)streamLength);
                if(hGlobal is 0) return E_OUTOFMEMORY;

                void* pLockedMemory = GlobalLock(hGlobal);
                if(pLockedMemory is null)
                {
                    _ = GlobalFree(hGlobal);
                    return E_FAIL;
                }

                int hResultDataCopy = S_OK;
                try
                {
                    _ = _comStream.Seek(0, (uint)SeekOrigin.Begin, null);

                    uint bytesRead  = 0;
                    hResultDataCopy = _comStream.Read((byte*)pLockedMemory, (uint)streamLength, &bytesRead);

                    if(hResultDataCopy is not S_OK || bytesRead < streamLength) hResultDataCopy = E_FAIL;

                }
                finally { _ = GlobalUnlock(hGlobal); }

                if(hResultDataCopy is not S_OK)
                {
                    _ = GlobalFree(hGlobal);
                    return hResultDataCopy;
                }

                pMedium->tymed          = TYMED.TYMED_HGLOBAL;
                pMedium->hGlobal        = hGlobal;
                pMedium->pUnkForRelease = (IUnknown.Native*)null;

                return S_OK;
            }
            private int HandleSetData(FORMATETC* pFormatetc, STGMEDIUM* pMedium, bool fRelease, ref STGMEDIUM? fieldToStore)
            {
                const int notSupportedTYMED  = 0;
                const TYMED SUPPORTED_TYMEDS = TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM;

                if((pFormatetc->tymed & SUPPORTED_TYMEDS) is notSupportedTYMED)
                {
#if DEBUG
                    if(Debugger.IsAttached is true) Debugger.Break();
#endif
                    return DV_E_TYMED;
                }

                STGMEDIUM mediumToStore;

                if(fRelease is true) mediumToStore = *pMedium;
                else
                {
                    if(pMedium->tymed == TYMED.TYMED_ISTREAM)
                    {
                        if(pMedium->pstm is null) return E_POINTER;

                        IStream.Native* pClonedStream = null;

                        const int IStream_Clone_VTableIndex = 13;
                        int hrClone = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, IStream.Native**, int>)(((void**)((ComInterfaceDispatch*)pMedium->pstm)->Vtable)[IStream_Clone_VTableIndex]))((ComInterfaceDispatch*)pMedium->pstm, &pClonedStream);

                        if(hrClone < S_OK) return hrClone;

                        mediumToStore = new STGMEDIUM 
                        { 
                            tymed          = TYMED.TYMED_ISTREAM, 
                            pstm           = pClonedStream,
                            pUnkForRelease = (IUnknown.Native*)pClonedStream
                        };
                    }
                    else
                    {
                        const int default_GMEM_MOVEABLE = 0;
                        nint hDuplicated = OleDuplicateData(pMedium->hGlobal, pFormatetc->cfFormat, default_GMEM_MOVEABLE);
                        if(hDuplicated == nint.Zero) return E_OUTOFMEMORY;

                        mediumToStore = new STGMEDIUM
                        {
                            tymed          = TYMED.TYMED_HGLOBAL,
                            hGlobal        = hDuplicated,
                            pUnkForRelease = null
                        };
                    }
                }

                if(fieldToStore.HasValue is true) { STGMEDIUM oldValue = fieldToStore.Value; ReleaseStgMedium(&oldValue); }

                fieldToStore = mediumToStore;

                return S_OK;
            }
            private unsafe int CreatePreferredDropEffect(STGMEDIUM* pMedium)
            {
                nuint sizeInBytes = sizeof(uint);

                nint hGlobal = GlobalAlloc(GMEM_MOVEABLE, sizeInBytes);
                if(hGlobal is 0) return E_OUTOFMEMORY;

                void* pLockedMemory = GlobalLock(hGlobal);
                if(pLockedMemory is null)
                {
                    _ = GlobalFree(hGlobal);
                    return E_FAIL;
                }

                try
                {
                    const uint DROPEFFECT_COPY = 1;
                    *(uint*)pLockedMemory = DROPEFFECT_COPY;
                }
                finally { _ = GlobalUnlock(hGlobal); }

                pMedium->tymed          = TYMED.TYMED_HGLOBAL;
                pMedium->hGlobal        = hGlobal;
                pMedium->pUnkForRelease = (IUnknown.Native*)null;

                return S_OK;
            }
#if DEBUG
            private static unsafe void LogUnsupportedFormat(FORMATETC* pFormatetc, [CallerMemberName]string? message = null)
            {
                ushort formatId = pFormatetc->cfFormat;

                const ushort CF_HDROP = 15;
                if(formatId == CF_HDROP || formatId == s_shellIdListArrayFormatId) return; // Expected unsupported format
                              
                string formatName;

                char[] buffer = new char[VirtualFileDragDrop.MAX_PATH];
                fixed(char* pBuffer = buffer)
                {
                    int length = GetClipboardFormatNameW(formatId, pBuffer, buffer.Length);
                    if(length > 0)
                    {
                        formatName = new string(buffer, 0, length);
                    }
                    else
                    {
                        formatName = formatId switch
                        {
                            1  => "CF_TEXT",
                            2  => "CF_BITMAP",
                            3  => "CF_METAFILEPICT",
                            4  => "CF_SYLK",
                            5  => "CF_DIF",
                            6  => "CF_TIFF",
                            7  => "CF_OEMTEXT",
                            8  => "CF_DIB",
                            9  => "CF_PALETTE",
                            10 => "CF_PENDATA",
                            11 => "CF_RIFF",
                            12 => "CF_WAVE",
                            13 => "CF_UNICODETEXT",
                            14 => "CF_ENHMETAFILE",
                            15 => "CF_HDROP",
                            16 => "CF_LOCALE",
                            17 => "CF_DIBV5",
                            _ => "Unknown/Unregistered"
                        };
                    }
                }
                Debug.WriteLine("---------------------------------------------");
                Debug.WriteLine($"Source method: {message}");
                Debug.WriteLine($"Format ID:     {formatId} ('{formatName}')");
                Debug.WriteLine($"Aspect:        {pFormatetc->dwAspect}");
                Debug.WriteLine($"Medium Type:   {pFormatetc->tymed}");
                Debug.WriteLine($"Index:         {pFormatetc->lindex}");
                Debug.WriteLine("---------------------------------------------");
            }
            [LibraryImport("user32")]
            private static partial int GetClipboardFormatNameW(uint format, char* lpszFormatName, int cchMaxCount);
#endif
            [LibraryImport("ole32")]
            private static unsafe partial void ReleaseStgMedium(STGMEDIUM* pmedium);
            [LibraryImport("ole32")]
            private static partial nint OleDuplicateData(nint hSrc, ushort cfFormat, uint uiFlags);
            [LibraryImport("ole32")]
            private static unsafe partial void CoTaskMemFree(void* pv);
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

                const int IUnknown_QueryInterface_VTableIndex = 0;
                int hResultQI_IEnumFORMATETC;
                fixed(Guid* pIID = &IID_IEnumFORMATETC) hResultQI_IEnumFORMATETC =/*QueryInterface*/((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*/*implicitThis*/, Guid*/*riid*/, void**/*ppvObject*/, int/*HRESULT*/>)((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[IUnknown_QueryInterface_VTableIndex])((ComInterfaceDispatch*)pUnknown, pIID, (void**)ppEnum);
                
                _ = Marshal.Release(pUnknown);
                return hResultQI_IEnumFORMATETC;
            }
            public int Next(uint celt, FORMATETC* rgelt, uint* pceltFetched)
            {
                if(rgelt is null) return E_POINTER;

                if(pceltFetched is null)
                {
                    if(celt > 1) return E_POINTER;
                }
                else { *pceltFetched = 0; }

                uint fetchedCount = 0;
                while(fetchedCount < celt && _currentIndex < _formats.Length)
                {
                    rgelt[fetchedCount] = _formats[_currentIndex];

                    fetchedCount++;
                    _currentIndex++;
                }
                if(pceltFetched is not null) *pceltFetched = fetchedCount;
                
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
        private sealed unsafe partial class ReadOnlyMemoryComStream : IStream
        {
            private const int STG_E_ACCESSDENIED                        = unchecked((int)0x80030005);
            private const int STG_E_INVALIDFUNCTION                     = unchecked((int)0x80030001);
            private const int STG_E_READFAULT                           = unchecked((int)0x8003001D);
            private const int STG_E_INVALIDFLAG                         = unchecked((int)0x800300FF);
            private readonly string                   _streamName;
            private readonly StrategyBasedComWrappers _localComWrappers;
            private readonly ArraySegment<byte>       _buffer;
            private          long                     _position;

            public ReadOnlyMemoryComStream(MemoryStream managedStream, string streamName, StrategyBasedComWrappers localComWrappers)
            {
                _localComWrappers = localComWrappers;
                _streamName       = streamName;
                _position         = managedStream.Position; 

                if(managedStream.TryGetBuffer(out _buffer) is false) _buffer = new ArraySegment<byte>(managedStream.ToArray());                
            }
            private ReadOnlyMemoryComStream(ArraySegment<byte> buffer, long position, string streamName, StrategyBasedComWrappers localComWrappers)
            {
                _buffer           = buffer;
                _position         = position;
                _streamName       = streamName;
                _localComWrappers = localComWrappers;
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public unsafe int Read(byte* pv, uint cb, uint* pcbRead)
            {
                if(pv is null) return E_POINTER;
                if(pcbRead is not null) *pcbRead = 0;
                if(cb is 0) return S_OK;

                long remainingBytes = _buffer.Count - _position;
                if(remainingBytes <= 0) return S_FALSE; 

                int bytesToRead        = (int)Math.Min(cb, (ulong)remainingBytes);

                Span<byte> source      = _buffer.AsSpan((int)_position, bytesToRead);

                Span<byte> destination = new Span<byte>(pv, bytesToRead);

                source.CopyTo(destination);

                _position += bytesToRead;

                if(pcbRead is not null) *pcbRead = (uint)bytesToRead;
                
                return (bytesToRead < cb) ? S_FALSE : S_OK;
            }
            public int Write(byte* pv, uint cb, uint* pcbWritten) => _ = STG_E_ACCESSDENIED;
            public int Seek(long dlibMove, uint dwOrigin, ulong* plibNewPosition)
            {
                long newPosition;
                long streamLength = _buffer.Count;

                switch((SeekOrigin)dwOrigin)
                {
                    case SeekOrigin.Begin:
                    newPosition = dlibMove;
                    break;

                    case SeekOrigin.Current:
                    newPosition = _position + dlibMove;
                    break;

                    case SeekOrigin.End:
                    newPosition = streamLength + dlibMove;
                    break;

                    default: return STG_E_INVALIDFUNCTION;
                }

                if(newPosition < 0) return E_FAIL;

                _position = newPosition;

                if(plibNewPosition is not null) *plibNewPosition = (ulong)_position;
                
                return S_OK;
            }
            public int SetSize(ulong libNewSize) => _ = STG_E_ACCESSDENIED;
            public int CopyTo(IStream.Native* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
            {
                if(pstm is null) return E_POINTER;
                if(pcbRead is not null) *pcbRead = 0;
                if(pcbWritten is not null) *pcbWritten = 0;

                long remainingBytes = _buffer.Count - _position;
                long bytesToCopy    = (long)Math.Min(cb, (ulong)remainingBytes);

                if(bytesToCopy <= 0) return S_OK;

                uint bytesWrittenInStep = 0;
                const int ISequentialStream_Write_VTableIndex = 4;
                int hResultWriteToStream;
                fixed(byte* pSource = _buffer.AsSpan((int)_position, (int)bytesToCopy)) hResultWriteToStream = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, byte*, uint, uint*, int>)(((void**)((ComInterfaceDispatch*)pstm)->Vtable)[ISequentialStream_Write_VTableIndex]))((ComInterfaceDispatch*)pstm, pSource, (uint)bytesToCopy, &bytesWrittenInStep);
                
                if(hResultWriteToStream is not S_OK) return hResultWriteToStream;

                _position += bytesWrittenInStep;

                if(pcbRead is not null)    *pcbRead    = bytesWrittenInStep;
                if(pcbWritten is not null) *pcbWritten = bytesWrittenInStep;

                const int STG_E_WRITEFAULT = unchecked((int)0x8003001E);

                return (bytesWrittenInStep < bytesToCopy) ? STG_E_WRITEFAULT : S_OK;
            }
            public int Commit(uint grfCommitFlags) => _ = S_OK;
            public int Revert() => _ = S_OK;
            public int LockRegion(ulong libOffset, ulong cb, uint dwLockType)   => _ = E_NOTIMPL;
            public int UnlockRegion(ulong libOffset, ulong cb, uint dwLockType) => _ = E_NOTIMPL;
            public int Stat(STATSTG* pstatstg, STATFLAG grfStatFlag)
            {
                if(pstatstg is null) return E_POINTER;

                *pstatstg = default;

                pstatstg->type   = STGTY.STGTY_STREAM;
                pstatstg->cbSize = (ulong)_buffer.Count;

                switch(grfStatFlag)
                {
                    case STATFLAG.STATFLAG_DEFAULT:
                    case STATFLAG.STATFLAG_NOOPEN:
                        nuint sizeInBytes = (nuint)(_streamName.Length + 1) * sizeof(char);
                        char* pName       = (char*)CoTaskMemAlloc(sizeInBytes);
                        if(pName is null) return E_OUTOFMEMORY;
        
                        _streamName.CopyTo(new Span<char>(pName, _streamName.Length));
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
                *ppstm = null;

                ReadOnlyMemoryComStream cloneInstance = new ReadOnlyMemoryComStream(_buffer, _position, _streamName, _localComWrappers);

                nint pUnknown = _localComWrappers.GetOrCreateComInterfaceForObject(cloneInstance, CreateComInterfaceFlags.None);
                if(pUnknown == nint.Zero) return E_FAIL;

                const int IUnknown_QueryInterface_VTableIndex = 0;
                int hResultQI_IStream;
                try { fixed(Guid* pIID = &IID_IStream) hResultQI_IStream = ((delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Guid*, void**, int>)(((void**)((ComInterfaceDispatch*)pUnknown)->Vtable)[IUnknown_QueryInterface_VTableIndex]))((ComInterfaceDispatch*)pUnknown, pIID, (void**)ppstm); }
                finally { _ = Marshal.Release(pUnknown); }

                return hResultQI_IStream;
            }
        }
        [GeneratedComClass]
        private sealed partial class DropSource : IDropSource /*IDropSourceNotify*/ /*ICustomQueryInterface*/
        {
            private const int  DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
            private const int  DRAGDROP_S_DROP              = 0x00040100;
            private const int  DRAGDROP_S_CANCEL            = 0x00040101;
            private const uint MK_LBUTTON                   = 0x0001;
            private const uint MK_RBUTTON                   = 0x0002;

            public DropSource() { }
            public int QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool fEscapePressed, uint grfKeyState)
            {
                if(fEscapePressed is true)                         return DRAGDROP_S_CANCEL;
                if((grfKeyState & (MK_LBUTTON | MK_RBUTTON)) is 0) return DRAGDROP_S_DROP;

                return S_OK;
            }
            public int GiveFeedback(uint dwEffect) => _ = DRAGDROP_S_USEDEFAULTCURSORS;
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
        [GeneratedComInterface, Guid("DE5BF786-477A-11d2-839A-00C04FD918D0")]
        public unsafe partial interface IDragSourceHelper : IUnknown
        {
            [PreserveSig]
            int InitializeFromBitmap(SHDRAGIMAGE* pshdi, IDataObject.Native* pDataObject);
            [PreserveSig]
            int InitializeFromWindow(nint hwnd, POINT* ppt, IDataObject.Native* pDataObject);
            new struct Native { }
        }
        [GeneratedComInterface, Guid("83E07D0D-0C5F-4163-BF1A-60B274051E40")]
        public unsafe partial interface IDragSourceHelper2 : IDragSourceHelper
        {
            [PreserveSig]
            int SetFlags(uint dwFlags);
            new struct Native { }
        }
        public readonly struct DragDropOptions
        {
            public POINT CursorOffset { get; init; }
            public nint HBitmap { get; init; }
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int    bmType;          // Не используется
            public int    bmWidth;         // Ширина битмапа в пикселях
            public int    bmHeight;        // Высота битмапа в пикселях
            public int    bmWidthBytes;    // Количество байт в одной строке пикселей
            public ushort bmPlanes;     // Не используется
            public ushort bmBitsPixel;  // Количество бит на пиксель
            public nint   bmBits;         // Указатель на пиксельные данные (не используется GetObjectW)
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct SHDRAGIMAGE
        {
            public SIZE  sizeDragImage;
            public POINT ptOffset;
            public nint  hbmpDragImage;
            public uint  crColorKey;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
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
            public uint   tdSize;
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
        private enum DROPIMAGETYPE
        {
            /// <summary>An invalid drop image type.</summary>
            DROPIMAGE_INVALID = -1,
            /// <summary>No drop image is displayed.</summary>
            DROPIMAGE_NONE = 0,
            /// <summary>A copy operation drop image (e.g., a plus sign).</summary>
            DROPIMAGE_COPY = 1,
            /// <summary>A move operation drop image.</summary>
            DROPIMAGE_MOVE = 2,
            /// <summary>A link operation drop iGiveFeedbackmage (e.g., an arrow).</summary>
            DROPIMAGE_LINK = 4,
            /// <summary>A label decoration drop image.</summary>
            DROPIMAGE_LABEL = 6,
            /// <summary>A warning decoration drop image (e.g., an exclamation point).</summary>
            DROPIMAGE_WARNING = 7,
            /// <summary>Windows 7 and later. Do not display a drop image.</summary>
            DROPIMAGE_NOIMAGE = 8,
        }
        [LibraryImport("user32")]
        private static partial uint RegisterClipboardFormatW([MarshalAs(UnmanagedType.LPWStr)] string lpszFormat);
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const int  E_OUTOFMEMORY = unchecked((int)0x8007000E);
        [LibraryImport("kernel32")]
        private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);
        [LibraryImport("kernel32")]
        private static partial nint GlobalFree(nint hMem);
        [LibraryImport("kernel32")]
        private static unsafe partial void* GlobalLock(nint hMem);
        [LibraryImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalUnlock(nint hMem);
        [LibraryImport("ole32")]
        private static unsafe partial void* CoTaskMemAlloc(nuint cb);
        [LibraryImport("ole32")]
        private static unsafe partial int DoDragDrop(IDataObject.Native* pDataObj, IDropSource.Native* pDropSource, uint dwOKEffects, uint* pdwEffect);
        private const uint CLSCTX_INPROC_SERVER = 0x1;
        [LibraryImport("ole32")]
        private static unsafe partial int CoCreateInstance(Guid* rclsid, IUnknown.Native* pUnkOuter, uint dwClsContext, Guid* riid, IUnknown.Native** ppv);
        [LibraryImport("gdi32")]
        private static partial int GetObjectW(nint hGdiObject, int cbBuffer, void* lpvObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct DROPDESCRIPTION
        {
            public DROPIMAGETYPE type;
            public fixed char szMessage[VirtualFileDragDrop.MAX_PATH];
            public fixed char szInsert [VirtualFileDragDrop.MAX_PATH];
        }
    }
}