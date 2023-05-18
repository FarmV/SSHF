using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using FVH.Background.Input;

using Microsoft.Win32.SafeHandles;

using ReactiveUI;

using SSHF.Views.Windows.Notify;

namespace SSHF.Infrastructure.SharedFunctions
{

    public interface IWindowPositionUpdater
    {
        bool IsUpdateWindow { get; }
        Task UpdateWindowPos(CancellationToken token);
        void DargMove();
    }


    internal class Win32WPFWindowPositionUpdater : ReactiveUI.ReactiveObject, IWindowPositionUpdater
    {
        private readonly Window _window;
        private Dispatcher _dispatcher;
        private bool _isUpdateWindow;
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
        internal async Task UpdateWindowPositionRelativeToCursor(CancellationToken token)
        {
            try
            {
                int SizeWindow = -1;
                int margin = 30;
                int notMessage_WM_WINDOWPOSCHANGING = 0x0400;
                int ignorNewSizeWindow = 0x0001;

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
                        SetWindowPos(helper.Handle, 0, Convert.ToInt32(point.X - margin), Convert.ToInt32(point.Y - margin),
                            SizeWindow, SizeWindow, notMessage_WM_WINDOWPOSCHANGING | ignorNewSizeWindow);
                    }, System.Windows.Threading.DispatcherPriority.Render, token);
                }
            }
            finally
            {
                IsUpdateWindow = false;
            }
        }
        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr handle, int handle2, int x, int y, int cx, int cy, int flag);
        public Task UpdateWindowPos(CancellationToken token) => UpdateWindowPositionRelativeToCursor(token);
        public void DargMove()
        {
            _window.Dispatcher.Invoke(() =>
            {
                if (Mouse.LeftButton is MouseButtonState.Pressed)
                {
                    _window.DragMove();
                }

            });
        }

    }
}

