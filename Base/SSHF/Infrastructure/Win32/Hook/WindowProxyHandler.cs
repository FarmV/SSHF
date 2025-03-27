using System;
using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;


namespace FVH.SSHF.Infrastructure.Win32
{
    internal partial class WindowProxyHandler : IDisposable
    {
        private bool _isDisposed = false;
        private const long WS_POPUP = 0x80000000L;
        protected HwndSource _proxyInputHandlerWindow;
        protected readonly Dispatcher _dispatcher;
        internal WindowProxyHandler(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _dispatcher.Invoke(() =>
            {
                HwndSourceParameters configInitWindow = new HwndSourceParameters(name: $"{nameof(FVH)}.{nameof(WindowProxyHandler)}-{Path.GetRandomFileName}", width: 0, height: 0)
                {
                    WindowStyle = unchecked((int)WS_POPUP)
                };
                _proxyInputHandlerWindow = new HwndSource(configInitWindow);
            });
            ArgumentNullException.ThrowIfNull(_proxyInputHandlerWindow);
        }    
        public void Dispose()
        {
            Dispose(isManagedContext: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool isManagedContext)
        {
            if(_isDisposed) return;
            
            if(isManagedContext) _dispatcher.Invoke(() => _proxyInputHandlerWindow.Dispose());
                        
            _isDisposed = true;
        }
        protected void AddHandler(HwndSourceHook hwndSourceHook) => _dispatcher.Invoke(() => _proxyInputHandlerWindow.AddHook(hwndSourceHook));                       
        protected void RemoveHandler(HwndSourceHook hwndSourceHook) => _dispatcher.Invoke(() => _proxyInputHandlerWindow.RemoveHook(hwndSourceHook));                 
    }
}