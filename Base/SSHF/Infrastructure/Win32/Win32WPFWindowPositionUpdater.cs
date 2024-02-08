using ControlzEx.Standard;

using ReactiveUI;

using SSHF.Infrastructure.Interfaces;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;


namespace SSHF.Infrastructure
{
    internal partial class Win32WPFWindowPositionUpdater : ReactiveUI.ReactiveObject, IWindowPositionUpdater
    {
        private const int IGNORE_SIZE_WINDOW = -1;
        private const int HWND_TOP = 0;
        private const int NOT_MESSAGE_WM_WINDOWPOSCHANGING = 0x0400;
        private const int SWP_NOSIZE = 0x0001;
        private const int OFFSET_CURSOR = 30;
        private readonly Window _window;
        private nint _handleWindow;
        private bool _isUpdateWindow;
        private Point _lastPontCursor = default;
        public Win32WPFWindowPositionUpdater(Window window)
        {
            _window = window;
            _handleWindow = new WindowInteropHelper(_window).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(_handleWindow);
            hwndSource.AddHook(WndProc);
        }
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x46:
                    if (Mouse.LeftButton != MouseButtonState.Pressed)
                    {
                        WINDOWPOS wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                        wp.flags |= SWP.NOMOVE;
                        Marshal.StructureToPtr(wp, lParam, false);
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        public bool IsUpdateWindow
        {
            get => _isUpdateWindow;
            private set => this.RaiseAndSetIfChanged(ref _isUpdateWindow, value);
        }
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);
        public async Task DargMove() => await _window.Dispatcher.InvokeAsync(() =>
        {
            if (_isUpdateWindow is true) throw new InvalidOperationException($"The window drag operation cannot be invoked while the window is being updated. Check {nameof(IsUpdateWindow)} property");

            IsUpdateWindow = true;
            _window.DragMove();
            IsUpdateWindow = false;
        });
        private async Task UpdateWindowPositionRelativeToCursor(CancellationToken сancelToken)
        {
            if (сancelToken.IsCancellationRequested is true) return;
            if (_isUpdateWindow is true) throw new InvalidOperationException($"The window refresh operation cannot be invoked while the window is being refreshed. Check {nameof(IsUpdateWindow)} property");
            if (Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to set the timer range");
            try
            {
                IsUpdateWindow = true;

                while (сancelToken.IsCancellationRequested is not true)
                {
                    if (сancelToken.IsCancellationRequested is true) return;
                    Point currentPointCursor = Win32Cursor.GetCursorPosition();
                    if (_lastPontCursor == default || _lastPontCursor != currentPointCursor)
                    {
                        await _window.Dispatcher.InvokeAsync(() =>
                        {
                            if (Thread.CurrentThread.Priority is ThreadPriority.Normal) Thread.CurrentThread.Priority = ThreadPriority.Highest;
                            SetWindowPos(_handleWindow, HWND_TOP, Convert.ToInt32(currentPointCursor.X - OFFSET_CURSOR), Convert.ToInt32(currentPointCursor.Y - OFFSET_CURSOR),
                                IGNORE_SIZE_WINDOW, IGNORE_SIZE_WINDOW, NOT_MESSAGE_WM_WINDOWPOSCHANGING | SWP_NOSIZE);
                        }, System.Windows.Threading.DispatcherPriority.Render, сancelToken);
                    }
                }
                if (Win32TimePeriod.TimeEndPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to change the timer range");
            }
            finally
            {
                await _window.Dispatcher.InvokeAsync(() =>
                {
                    if (Thread.CurrentThread.Priority is ThreadPriority.Highest) Thread.CurrentThread.Priority = ThreadPriority.Normal;
                }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None);

                IsUpdateWindow = false;
            }
        }
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, int handle2, int x, int y, int cx, int cy, int flag);
    }
}

