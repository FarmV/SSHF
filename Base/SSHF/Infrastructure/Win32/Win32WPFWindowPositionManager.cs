using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;


using R3;

namespace FVH.SSHF.Infrastructure
{  
    public sealed partial class Win32WPFWindowPositionManager
    {
        private readonly System.Windows.Window _window;
        private readonly WPFDpiCorrector _dpiCorrector;
        private WindowMetrics? _currentMetrics;
        private bool _isUpdateWindow = false;     
        public Win32WPFWindowPositionManager(System.Windows.Window window, WPFDpiCorrector dpiCorrector)
        {
            _window       = window;
            _dpiCorrector = dpiCorrector;
     
            if(window.Dispatcher.CheckAccess() is true) SetUnsafe(this, _window);
            else { window.Dispatcher.Invoke(() => SetUnsafe(this, _window)); }

            static void SetUnsafe(Win32WPFWindowPositionManager manager, System.Windows.Window window)
            {
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source.AddHook(manager.OverrideLogicToChangeWindowPosition);
            }
        }
        
        private unsafe nint OverrideLogicToChangeWindowPosition(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            const int WM_WINDOWPOSCHANGING = 0x0046;
            if(msg is not WM_WINDOWPOSCHANGING) return nint.Zero;

            if(Mouse.LeftButton is not MouseButtonState.Pressed)
            {
                ref WINDOWPOS windowPos = ref Unsafe.AsRef<WINDOWPOS>((void*)lParam);
                if(windowPos.y is 0)
                {
                    windowPos.flags |= SetWindowPosFlags.SWP_NOMOVE;
                }
            }

            return nint.Zero;
        }
        public async ValueTask DragMove()
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

            if(_window.Dispatcher.CheckAccess() is false)
            {
                await await _window.Dispatcher.InvokeAsync(DragMove);
                return;
            }

            bool lockAcquired = false;

            if(Interlocked.CompareExchange(ref _isUpdateWindow, true, false) is false) { lockAcquired = true; }
            else
            {
                RaiseMouseEvent();

                Stopwatch sw = Stopwatch.StartNew();
                long timeout  = Stopwatch.Frequency / 10; // 100 ms

                while(true)
                {
                    await Task.Yield();

                    if(Interlocked.CompareExchange(ref _isUpdateWindow, true, false) is false)
                    {
                        lockAcquired = true;
                        break; 
                    }

                    if(sw.ElapsedTicks >= timeout) break; 
                }

            }

            if(lockAcquired is true)
            {
                try { if(Mouse.LeftButton is MouseButtonState.Pressed) _window.DragMove(); }
                finally { Volatile.Write(ref _isUpdateWindow, false); }
            }
        }
        public bool IsUpdateWindow { get => Volatile.Read(ref _isUpdateWindow); }
        private void UpdateMetrics() => Volatile.Write(ref _currentMetrics, GetMetrics());       
        public WindowMetrics GetMetrics() 
        {
            if(_window.Dispatcher.CheckAccess() is false) return _window.Dispatcher.Invoke(GetMetrics);
            PresentationSource source = PresentationSource.FromVisual(_window);

            System.Windows.Media.Matrix toDevice   = source.CompositionTarget.TransformToDevice;
            System.Windows.Media.Matrix fromDevice = source.CompositionTarget.TransformFromDevice;

            double logicalWidth  = _window.ActualWidth;
            double logicalHeight = _window.ActualHeight;

            Vector physicalSize   = toDevice.Transform(new Vector(logicalWidth, logicalHeight));
            double physicalWidth  = physicalSize.X;
            double physicalHeight = physicalSize.Y;

            nint handle = new System.Windows.Interop.WindowInteropHelper(_window).Handle;

            return new WindowMetrics(handle, toDevice, fromDevice, physicalWidth, physicalHeight);
        }
        public async Task SetPositionWindowToCursor(WindowMetrics metrics, Point pointCursor)
        {
            Point logicalCursorPos = metrics.ToDeviceTransform.Transform(pointCursor);

            int desiredX = (int)(logicalCursorPos.X - metrics.PhysicalWidth / 2);
            int desiredY = (int)(logicalCursorPos.Y - metrics.PhysicalHeight / 2);

            const int ignoreSizeWindowStub = -1;
            const int SWP_NOSIZE = 0x0001;
            const int SWP_NOSENDCHANGING = 0x0400;
            const int HWND_TOP = 0;
            await _window.Dispatcher.InvokeAsync(new Action(() =>_ = SetWindowPos(new WindowInteropHelper(_window).Handle, HWND_TOP, desiredX, desiredY, ignoreSizeWindowStub, ignoreSizeWindowStub, SWP_NOSIZE | SWP_NOSENDCHANGING)));
        }
        public async Task UpdateWindowPositionRelativeToCursor(CancellationToken cancelToken)
        {
            IDisposable? subscription = null;
            try
            {
                await _window.Dispatcher.InvokeAsync(() =>
                {
                    subscription = _dpiCorrector.ChangeDpiCurrentWindow.Subscribe((DpiScale dpiScale) => UpdateMetrics());
             
                    UpdateMetrics();
                });

                if(cancelToken.IsCancellationRequested is true) return;

                await Task.Run(() => PositionUpdateLoop(cancelToken), CancellationToken.None);
            }
            finally
            {
                subscription?.Dispose();
                Volatile.Write(ref _currentMetrics, null);
            }
        }
        private void PositionUpdateLoop(CancellationToken cancelToken)
        {
            [DoesNotReturn] static void ThrowHelper(string m) => throw new InvalidOperationException(m);

            const int VK_LBUTTON = 0x01;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool CheckMouseLeftDown() => (GetKeyState(VK_LBUTTON) & 0x8000) is not 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void RaiseMouseEvent()
            {
                MouseButtonEventArgs mouseEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                    Source      = _window
                };
                ((UIElement)_window.Content).RaiseEvent(mouseEvent);
            }

            Thread.CurrentThread.Priority = ThreadPriority.Highest;
           
            if(Interlocked.CompareExchange(ref _isUpdateWindow, true, false) is true)
            {
                ThrowHelper($"The window refresh operation cannot be invoked while it is already running. Check {nameof(_isUpdateWindow)} property.");
            }
            if(Win32TimePeriod.TimeBeginPeriod(Win32TimePeriod.MinimumTimerResolution) is not Win32TimePeriod.TIMERR_NOERROR)
            {
                Volatile.Write(ref _isUpdateWindow, false);
                ThrowHelper("Failed to set the timer resolution.");
            }

            Point lastPointCursor = default;

            try
            {
                while(cancelToken.IsCancellationRequested is false)
                {
                    WindowMetrics? metrics = Volatile.Read(ref _currentMetrics);
                    if(metrics is null)
                    {
                        _ = Thread.Yield();
                        continue;
                    }

                    if(CheckMouseLeftDown() is true) _window.Dispatcher.Invoke(RaiseMouseEvent);
                   
                    Point currentPointCursor = Win32Cursor.GetCursorPosition();

                    if(lastPointCursor == currentPointCursor) continue;

                    lastPointCursor = currentPointCursor;
                  
                    Point logicalCursorPos = metrics.ToDeviceTransform.Transform(currentPointCursor);

                    int desiredX = (int)(logicalCursorPos.X - metrics.PhysicalWidth / 2);
                    int desiredY = (int)(logicalCursorPos.Y - metrics.PhysicalHeight / 2);

                    const int ignoreSizeWindowStub = -1;
                    const int SWP_NOSIZE           = 0x0001;
                    const int SWP_NOSENDCHANGING   = 0x0400;
                    const int SWP_NOACTIVATE       = 0x0010;
                    const int HWND_TOP = 0;

                    _ = SetWindowPos(metrics.Handle, HWND_TOP, desiredX, desiredY, ignoreSizeWindowStub, ignoreSizeWindowStub, SWP_NOSIZE | SWP_NOSENDCHANGING | SWP_NOACTIVATE);
                }
            }
            finally
            {
                _ = Win32TimePeriod.TimeEndPeriod(Win32TimePeriod.MinimumTimerResolution);
                Volatile.Write(ref _isUpdateWindow, false);
            }
        }

        public sealed record WindowMetrics(
        nint Handle,
        System.Windows.Media.Matrix ToDeviceTransform,
        System.Windows.Media.Matrix FromDeviceTransform,
        double PhysicalWidth,
        double PhysicalHeight);

        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(nint hwnd, out RECT LPRECT);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [LibraryImport("user32")]
        private static partial short GetKeyState(int nVirtKey);
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(nint handle, nint handle2, int x, int y, int cx, int cy, int flag);


        /// <summary>
        /// Содержит информацию о размере и положении окна. 
        /// Используется в сообщениях WM_WINDOWPOSCHANGING и WM_WINDOWPOSCHANGED.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            /// <summary>
            /// Дескриптор окна, которое будет вставлено перед текущим в Z-порядке.
            /// Этот параметр может быть дескриптором окна или одним из значений HWND_*.
            /// </summary>
            public nint hwndInsertAfter;

            /// <summary>
            /// Дескриптор окна.
            /// </summary>
            public nint hwnd;

            /// <summary>
            /// Горизонтальная позиция левого верхнего угла окна (x).
            /// Для дочерних окон позиция указывается относительно клиентской области родительского окна.
            /// </summary>
            public int x;

            /// <summary>
            /// Вертикальная позиция левого верхнего угла окна (y).
            /// Для дочерних окон позиция указывается относительно клиентской области родительского окна.
            /// </summary>
            public int y;

            /// <summary>
            /// Новая ширина окна в пикселях (cx).
            /// </summary>
            public int cx;

            /// <summary>
            /// Новая высота окна в пикселях (cy).
            /// </summary>
            public int cy;

            /// <summary>
            /// Флаги, определяющие параметры позиционирования окна. 
            /// Это комбинация значений SWP_* (SetWindowPos Flags).
            /// </summary>
            public SetWindowPosFlags flags;
        }
        [Flags]
        public enum SetWindowPosFlags : uint
        {
            /// <summary>
            /// Если установлено, окно получает фокус.
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

            /// <summary>
            /// Рисует рамку (определенную в классе окна) вокруг окна.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            /// Скрывает окно.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            /// Отбрасывает все содержимое клиентской области. Если этот флаг не указан,
            /// валидная часть клиентской области сохраняется и копируется в новый размер.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            /// Сохраняет текущую позицию (игнорирует параметры X и Y).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            /// Не изменяет владельца в Z-порядке.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

            /// <summary>
            /// Не перерисовывает изменения. Если этот флаг установлен, перерисовка любого
            /// вида не происходит. Это относится к клиентской области, неклиентской области
            /// (включая заголовок и полосы прокрутки) и любой части родительского окна,
            /// которая была "раскрыта" в результате перемещения окна.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            /// То же, что и SWP_NOOWNERZORDER.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            /// Не отправляет сообщение WM_WINDOWPOSCHANGING.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

            /// <summary>
            /// Сохраняет текущий размер (игнорирует параметры cx и cy).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            /// Сохраняет текущий Z-порядок (игнорирует параметр hwndInsertAfter).
            /// </summary>
            SWP_NOZORDER = 0x0004,

            /// <summary>
            /// Отображает окно.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040,

            /// <summary>
            /// Применяет асинхронное позиционирование. Предотвращает блокировку потока,
            /// если другой поток "заморожен".
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000
        }
    }
}