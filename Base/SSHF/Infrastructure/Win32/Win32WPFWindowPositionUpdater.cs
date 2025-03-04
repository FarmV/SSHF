using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Threading;

using ControlzEx.Standard;

using FVH.SSHF.Infrastructure.Interfaces;
using FVH.SSHF.FastWindowArea;
using FVH.SSHF.Infrastructure.Win32;

using ABI.System;
using R3;

namespace FVH.SSHF.Infrastructure
{
    internal partial class Win32WPFWindowPositionUpdater : IWindowPositionUpdater
    {
        private const nint HWND_TOP = 0;
        private const nint HWND_TOPMOST = -1;
        private const nint HWND_NOTOPMOST = -2;
        private const int IGNORE_SIZE_WINDOW = -1;
        private const int OFFSET_CURSOR = 30;
        private const int NOT_MESSAGE_WM_WINDOWPOSCHANGING = 0x0400;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int VK_LBUTTON = 0x01;
        private readonly System.TimeSpan _windowHideDelay = System.TimeSpan.FromMicroseconds(200);
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
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);      
#pragma warning restore CS0618
        public async Task DragMove()
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void RaiseMouseEvent() // Необходимо для того чтобы не приходилось 2 раза кликать
            {
                MouseButtonEventArgs mouseEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                    Source = _window
                };
                ((UIElement)_window.Content).RaiseEvent(mouseEvent);
            }
            if(_isUpdateWindow is true)
            {
                RaiseMouseEvent();
                await Task.Delay(32);
            }           
            IsUpdateWindow = true;
            _window.Dispatcher.Invoke(() =>
            {
                if(Mouse.LeftButton is MouseButtonState.Pressed) _window.DragMove();
            });
            IsUpdateWindow = false;
        }
        public bool IsUpdateWindow
        {
            get => _isUpdateWindow;
            private set
            {
                if(value == _isUpdateWindow) return;
                _isUpdateWindow = value;
            }
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
        private async Task UpdateWindowPositionRelativeToCursor(CancellationToken cancelToken)
        {            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool CheckMouseLeftDown() => ((GetKeyState(VK_LBUTTON) & 0x8000) != 0) is true;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void RaiseMouseEvent()
            {
                MouseButtonEventArgs mouseEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                    Source = _window
                };
                ((UIElement)_window.Content).RaiseEvent(mouseEvent);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateWindowPositionRelativeToCursor()
            {
                FastWindowViewModel model = _window.Dispatcher.Invoke(() =>
                {
                    if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is false) Thread.CurrentThread.StartUITimeCriticalSectionThrowIfNotUIThread();
                    return model =((FastWindow)_window).ViewModel ?? throw new NullReferenceException("model = MainWindowViewModel is null");
                });

                if(_isUpdateWindow is true) throw new InvalidOperationException($"The window refresh operation cannot be invoked while the window is being refreshed. Check {nameof(IsUpdateWindow)} property");
                if(Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to set the timer range");
                try
                {
                    IsUpdateWindow = true;
                   
                    while(cancelToken.IsCancellationRequested is not true)
                    {
                        if(CheckMouseLeftDown() is true) _window.Dispatcher.Invoke(RaiseMouseEvent);

                        if(cancelToken.IsCancellationRequested is true) return;
                        Point currentPointCursor = Win32Cursor.GetCursorPosition();
                        if(_lastPontCursor == default || _lastPontCursor != currentPointCursor)
                        {
                            if(MsScreenClip.IsEnableProcessHost())
                            {
                                Thread.Sleep(_windowHideDelay); // Задержка для того чтобы на скриншоте осталось окно 
                                model.HideWindow.Execute(R3.Unit.Default);
                            }
                            else
                            {
#if OneFastWindowNotTopMost
                                _window.Dispatcher.Invoke(() =>
                                {
                                    if(_window.Topmost != true) _window.Topmost = true;
                                });
#endif
                                if(model.VisibleCondition.CurrentValue is Visibility.Hidden) model.ShowWindow.Execute(R3.Unit.Default);
                               
                                _window.Dispatcher.Invoke(() =>
                                {
                                    SetWindowPos(_handleWindow, HWND_TOP, Convert.ToInt32(currentPointCursor.X /*- OFFSET_CURSOR*/ - _window.Width / 2 ), Convert.ToInt32(currentPointCursor.Y /*- OFFSET_CURSOR*/ - _window.Height / 2),
                                    IGNORE_SIZE_WINDOW, IGNORE_SIZE_WINDOW, SWP_NOSIZE | NOT_MESSAGE_WM_WINDOWPOSCHANGING);
                                });
                            }                        
                        }
                    }
                    if(Win32TimePeriod.TimeEndPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to change the timer range");
                }
                finally
                {
                    if(_window.Dispatcher.CheckAccess() is true)
                    {
                        if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is true) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread();
                    }
                    else
                    {
                        _window.Dispatcher.Invoke(() =>
                        {
                            if(Thread.CurrentThread.InUIThreadTimeCriticalSection() is true) Thread.CurrentThread.StopUITimeCriticalSectionThrowIfNotUIThread();
                        });
                    }
                    IsUpdateWindow = false;
                }
            }
            if(cancelToken.IsCancellationRequested is true) return;
            if(_window.Dispatcher.CheckAccess() is true) await Task.Run(UpdateWindowPositionRelativeToCursor, CancellationToken.None);
            else 
            {               
                 UpdateWindowPositionRelativeToCursor();               
            }           
        }
        [LibraryImport("User32")]
        [return: MarshalAs(UnmanagedType.I2)]
        private static partial short GetKeyState(int nVirtKey);
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, nint handle2, int x, int y, int cx, int cy, int flag);
    }
}

