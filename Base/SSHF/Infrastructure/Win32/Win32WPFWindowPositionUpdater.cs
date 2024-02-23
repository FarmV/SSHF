using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using ReactiveUI;
using ControlzEx.Standard;

using FVH.SSHF.Infrastructure.Interfaces;
using System.Diagnostics;
using FVH.SSHF.ViewModels.MainWindowViewModel;
using System.Reactive.Linq;
using System.Windows.Threading;


namespace FVH.SSHF.Infrastructure
{
    internal partial class Win32WPFWindowPositionUpdater : ReactiveUI.ReactiveObject, IWindowPositionUpdater
    {
        private const nint HWND_TOP = 0;
        private const int IGNORE_SIZE_WINDOW = -1;
        private const int OFFSET_CURSOR = 30;
        private const int NOT_MESSAGE_WM_WINDOWPOSCHANGING = 0x0400;
        private const int SWP_NOSIZE = 0x0001;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private readonly nint _handleWindow;
        private bool _isUpdateWindow;
        private Point _lastPontCursor = default;
        private readonly Window _window;
        public Win32WPFWindowPositionUpdater(Window window)
        {
            _window = window;
            _handleWindow = new WindowInteropHelper(_window).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(_handleWindow);
            hwndSource.AddHook(OverrideLogicToChangeWindowPosition);
        }
#pragma warning disable CS0618            
        private nint OverrideLogicToChangeWindowPosition(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool CheckAndIgnoreWindowTopPosition(ref WINDOWPOS newWindowPos)
            {
                if(newWindowPos.y is 0)
                {
                    newWindowPos.flags |= SWP.NOMOVE;
                    return true;
                }
                else { return false; }
            }
            if(msg is WM_WINDOWPOSCHANGING)
            {
                if(Mouse.LeftButton is not MouseButtonState.Pressed)
                {
                    WINDOWPOS wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    if(CheckAndIgnoreWindowTopPosition(ref wp) is not true) return nint.Zero;
                    Marshal.StructureToPtr(wp, lParam, false);
                }
            }
            return nint.Zero;
        }
#pragma warning restore CS0618
        public bool IsUpdateWindow
        {
            get => _isUpdateWindow;
            private set => this.RaiseAndSetIfChanged(ref _isUpdateWindow, value);
        }
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);
        public async Task DragMove() => await _window.Dispatcher.InvokeAsync(() =>
        {
            if(_isUpdateWindow is true) throw new InvalidOperationException($"The window drag operation cannot be invoked while the window is being updated. Check {nameof(IsUpdateWindow)} property");

            IsUpdateWindow = true;
            if(Mouse.LeftButton is MouseButtonState.Pressed) _window.DragMove();
            IsUpdateWindow = false;
        });
        private async Task UpdateWindowPositionRelativeToCursor(CancellationToken cancelToken)
        {
            MainWindowViewModel model = await _window.Dispatcher.InvokeAsync(() => model = ((IViewFor<MainWindowViewModel>)_window).ViewModel ?? throw new NullReferenceException("model = MainWindowViewModel is null"));

            if(cancelToken.IsCancellationRequested is true) return;
            if(_isUpdateWindow is true) throw new InvalidOperationException($"The window refresh operation cannot be invoked while the window is being refreshed. Check {nameof(IsUpdateWindow)} property");
            if(Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to set the timer range");
            try
            {
                IsUpdateWindow = true;

                nint lastMSHandle = nint.Zero;

                while(cancelToken.IsCancellationRequested is not true)
                {
                    if(cancelToken.IsCancellationRequested is true) return;
                    Point currentPointCursor = Win32Cursor.GetCursorPosition();
                    if(_lastPontCursor == default || _lastPontCursor != currentPointCursor)
                    {
                        await _window.Dispatcher.InvokeAsync(() =>
                        {
                            if(Thread.CurrentThread.Priority is ThreadPriority.Normal) Thread.CurrentThread.Priority = ThreadPriority.Highest;

                            if(MsScreenClip.IsEnableProcessHost())
                            {
                                Thread.Sleep(400); // Задержка для того чтобы на скриншоте осталось окно 
                                model.HideWindow.Execute().Wait();
                                return;
                            }                   
                            else
                            {
                                if(model.VisibleCondition is Visibility.Hidden) model.ShowWindow.Execute().Wait();

                                SetWindowPos(_handleWindow, HWND_TOP, Convert.ToInt32(currentPointCursor.X - OFFSET_CURSOR), Convert.ToInt32(currentPointCursor.Y - OFFSET_CURSOR),
                                IGNORE_SIZE_WINDOW, IGNORE_SIZE_WINDOW, NOT_MESSAGE_WM_WINDOWPOSCHANGING | SWP_NOSIZE);
                            }

                        }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None);
                    }
                }
                if(Win32TimePeriod.TimeEndPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to change the timer range");
            }
            finally
            {
                await _window.Dispatcher.InvokeAsync(() =>
                {
                    if(Thread.CurrentThread.Priority is ThreadPriority.Highest) Thread.CurrentThread.Priority = ThreadPriority.Normal;
                }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None);

                IsUpdateWindow = false;
            }
        }
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, nint handle2, int x, int y, int cx, int cy, int flag);
    }
}

