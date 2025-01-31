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
using static System.Windows.Forms.AxHost;
using R3;


namespace FVH.SSHF.Infrastructure
{
    internal partial class Win32WPFWindowPositionUpdater : IWindowPositionUpdater
    {
        private const nint HWND_TOP = 0;
        private const int IGNORE_SIZE_WINDOW = -1;
        private const int OFFSET_CURSOR = 30;
        private const int NOT_MESSAGE_WM_WINDOWPOSCHANGING = 0x0400;
        private const int SWP_NOSIZE = 0x0001;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int VK_LBUTTON = 0x01;
        private readonly System.TimeSpan _windowHidingDelay = System.TimeSpan.FromMicroseconds(200);
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
            //if(msg is 0x0201) 
            //{
            //   System.Diagnostics.Debugger.Break();
            //}
            //{
                //    if(Capt is false)
                //    {

                //        WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(_window);
                //        nint hWnd = wih.Handle;
                //        _ = SetCapture(hWnd);
                //        //nint res = GetCapture();
                //        //_ = ReleaseCapture();
                //    }

                //}

                return nint.Zero;
        }
      //  private bool Capt = false;
        [LibraryImport("User32")]
        [return: MarshalAs(UnmanagedType.SysInt)]
        private static partial nint SetCapture(nint hWnd);
        [LibraryImport("User32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ReleaseCapture();
        [LibraryImport("User32")]
        [return: MarshalAs(UnmanagedType.SysInt)]
        private static partial nint GetCapture();


#pragma warning restore CS0618

        public bool IsUpdateWindow
        {
            get => _isUpdateWindow;
            private set
            {
                if(value == _isUpdateWindow) return;
                _isUpdateWindow = value;
            }
        }
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);
        public Task DragMove() => _window.Dispatcher.Invoke(() =>
        {
            if(_isUpdateWindow is true) return Task.FromException(new InvalidOperationException($"The window drag operation cannot be invoked while the window is being updated. Check {nameof(IsUpdateWindow)} property"));

            IsUpdateWindow = true;
            if(Mouse.LeftButton is MouseButtonState.Pressed) _window.DragMove();
            IsUpdateWindow = false;
            return Task.CompletedTask;
        });


        [LibraryImport("User32")]
        [return: MarshalAs(UnmanagedType.I2)]
        private static partial short GetKeyState(int nVirtKey);


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
            async Task _updateWindowPositionRelativeToCursor()
            {
                FastWindowViewModel model = _window.Dispatcher.Invoke(() =>
                {
                    if(Thread.CurrentThread.InThreadUITimeCriticalSection() is false) Thread.CurrentThread.StartTimeCriticalSectionUI();
                    return model =((FastWindow)_window).ViewModel ?? throw new NullReferenceException("model = MainWindowViewModel is null");
                }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None);

                if(_isUpdateWindow is true) throw new InvalidOperationException($"The window refresh operation cannot be invoked while the window is being refreshed. Check {nameof(IsUpdateWindow)} property");
                if(Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to set the timer range");
                try
                {
                    IsUpdateWindow = true;

                   
                    while(cancelToken.IsCancellationRequested is not true)
                    {
#if DEBUG
                        App.Stopwatch.Restart();
#endif
                        if(CheckMouseLeftDown() is true) await _window.Dispatcher.InvokeAsync(RaiseMouseEvent,DispatcherPriority.Send,CancellationToken.None);
#if DEBUG
                        App.Stopwatch.Stop();
                        Debug.WriteLine($"{App.Stopwatch.ElapsedTicks}");
#endif

                        if(cancelToken.IsCancellationRequested is true) return;
                        Point currentPointCursor = Win32Cursor.GetCursorPosition();
                        if(_lastPontCursor == default || _lastPontCursor != currentPointCursor)
                        {
                            await _window.Dispatcher.InvokeAsync(async () =>
                            {
                                  if(MsScreenClip.IsEnableProcessHost())
                                  {
                                      Thread.Sleep(_windowHidingDelay); // Задержка для того чтобы на скриншоте осталось окно 
                                      model.HideWindow.Execute(R3.Unit.Default);
                                      return;
                                  }
                                  else
                                  {
                                      if(model.VisibleCondition.CurrentValue is Visibility.Hidden) model.ShowWindow.Execute(R3.Unit.Default);

                                      SetWindowPos(_handleWindow, HWND_TOP, Convert.ToInt32(currentPointCursor.X - OFFSET_CURSOR), Convert.ToInt32(currentPointCursor.Y - OFFSET_CURSOR),
                                      IGNORE_SIZE_WINDOW, IGNORE_SIZE_WINDOW, SWP_NOSIZE | NOT_MESSAGE_WM_WINDOWPOSCHANGING);
                                  }

                            }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None).Task.Unwrap();
                        }
                    }
                    if(Win32TimePeriod.TimeEndPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR) throw new InvalidOperationException("Failed to change the timer range");
                }
                finally
                {
                    if(_window.Dispatcher.CheckAccess() is true)
                    {
                        if(Thread.CurrentThread.InThreadUITimeCriticalSection() is true) Thread.CurrentThread.StopTimeCriticalSectionUI();
                    }
                    else
                    {
                        await _window.Dispatcher.InvokeAsync(() =>
                        {
                            if(Thread.CurrentThread.InThreadUITimeCriticalSection() is true) Thread.CurrentThread.StopTimeCriticalSectionUI();
                        }, System.Windows.Threading.DispatcherPriority.Render, CancellationToken.None);
                    }
                    IsUpdateWindow = false;
                }
            }
            if(cancelToken.IsCancellationRequested is true) return;
            if(_window.Dispatcher.CheckAccess() is true) await Task.Run(_updateWindowPositionRelativeToCursor, CancellationToken.None);
            else { await _updateWindowPositionRelativeToCursor(); }           
        }
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, nint handle2, int x, int y, int cx, int cy, int flag);
    }
}

