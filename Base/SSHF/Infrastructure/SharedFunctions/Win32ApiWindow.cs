using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using ReactiveUI;

namespace SSHF.Infrastructure.SharedFunctions
{
    public interface IWindowPositionUpdater
    {
        bool IsUpdateWindow { get; }
        Task UpdateWindowPos(CancellationToken token);
        void DargMove();
    }
    internal partial class Win32WPFWindowPositionUpdater : ReactiveUI.ReactiveObject, IWindowPositionUpdater
    {
        private readonly Window _window;
        private Dispatcher _dispatcher;
        private bool _isUpdateWindow;
        private const int IGNORE_SIZE_WINDOW = -1;
        private const int HWND_TOP = 0;
        private const int NOT_MESSAGE_WM_WINDOWPOSCHANGING = 0x0400;
        private const int SWP_NOSIZE = 0x0001;
        private const int OFFSET_CURSOR = 30;
        public Win32WPFWindowPositionUpdater(Window window)
        {
            _window = window;
            _dispatcher = _window.Dispatcher;
        }
        public bool IsUpdateWindow
        {
            get => _isUpdateWindow;
            private set => this.RaiseAndSetIfChanged(ref _isUpdateWindow, value);
        }
        public Dispatcher Dispatcher
        {
            get => _dispatcher;
            private set => this.RaiseAndSetIfChanged(ref _dispatcher, value);
        }
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);
        public void DargMove() => _window.Dispatcher.Invoke(() =>
        {
            if (Mouse.LeftButton is MouseButtonState.Pressed) _window.DragMove();
        });
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);
        private async Task UpdateWindowPositionRelativeToCursor(CancellationToken token)
        {
            if(NativeTimerHelper.TimeBeginPeriod(1) is not NativeTimerHelper.TIMERR_NOERROR) throw new InvalidOperationException("Failed to set the timer range");
            try
            {
                if (IsUpdateWindow is true) throw new InvalidOperationException("You cannot update a window that is already being updated");
                IsUpdateWindow = true;

                WindowInteropHelper helper = new WindowInteropHelper(_window);

                while (token.IsCancellationRequested is not true)
                {                  
                    await Task.Delay(1, token); //меньше миллисекунды не приходят эвенты мыши.

                    if (token.IsCancellationRequested is true) return;
                    Point point = CursorFunctions.GetCursorPosition();
                    await _window.Dispatcher.InvokeAsync(() =>
                    {
                        SetWindowPos(helper.Handle, HWND_TOP, Convert.ToInt32(point.X - OFFSET_CURSOR), Convert.ToInt32(point.Y - OFFSET_CURSOR),
                            IGNORE_SIZE_WINDOW, IGNORE_SIZE_WINDOW, NOT_MESSAGE_WM_WINDOWPOSCHANGING | SWP_NOSIZE);
                    }, System.Windows.Threading.DispatcherPriority.Render, token);
                }
                if (NativeTimerHelper.TimeEndPeriod(1) is not NativeTimerHelper.TIMERR_NOERROR) throw new InvalidOperationException("Failed to change the timer range");
            }
            finally
            {
                IsUpdateWindow = false;                
            }
        }
        public partial class NativeTimerHelper
        {
            public const uint TIMERR_NOERROR = 0;
            public const uint TIMERR_NOCANDO = 93;

            [LibraryImport("winmm", EntryPoint = "timeBeginPeriod")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static partial uint TimeBeginPeriod(uint uMilliseconds);
            [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static partial uint TimeEndPeriod(uint uMilliseconds);
        }
    }
}

