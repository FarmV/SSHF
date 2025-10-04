using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using FVH.Background.Input.Infrastructure.Interfaces;
using FVH.SSHF.Infrastructure;
using FVH.SSHF.Infrastructure.Interfaces;

using R3;

namespace FVH.SSHF.FastWindowArea
{
    internal class FastWindowCommand
    {
        private readonly System.Windows.Window _window;
        private bool _isExecutePresentNewImage = false;
        internal FastWindowCommand(System.Windows.Window window, FastWindowViewModel mainWindowViewModel)
        {
            _window = window;
            MainWindowViewModel = mainWindowViewModel;
        }
        public bool IsExecutePresentNewImages { get => Volatile.Read(ref _isExecutePresentNewImage); }    
        public FastWindowViewModel MainWindowViewModel { get; }     
        public async ValueTask PresentNewImage()
        {
            _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

            if(Interlocked.CompareExchange(ref _isExecutePresentNewImage, true, false) is not false) return;

            try
            {
                _ = SynchronizationContext.Current.StartSafeUITimeCriticalSection();

                if(await MainWindowViewModel.SetNewImage() is false) return;

                await MainWindowViewModel.PositionManager.SetPositionWindowToCursor(MainWindowViewModel.PositionManager.GetMetrics(), Win32Cursor.GetCursorPosition());

                MainWindowViewModel.ShowWindow();
                await MainWindowViewModel.WindowUpdater();

            }
            catch(Exception ex) { _ = Task.Run(() => ExceptionDispatchInfo.Capture(ex).Throw()); }
            finally { Volatile.Write(ref _isExecutePresentNewImage, false); }

            _ = Task.Run(() => Volatile.Write(ref _isExecutePresentNewImage, false));
        }
        public ValueTask SwitchBlockRefreshWindow()
        {
            MainWindowViewModel.SwitchBlockRefresh();
            return ValueTask.CompletedTask;
        }
        public bool CanExecuteStopRefreshWindow() => MainWindowViewModel.CanExecuteStopRefreshWindow();
        public async ValueTask StopRefreshWindow()
        {
            await MainWindowViewModel.StopUpdateWindow();
        }
        public ValueTask InvokeMsScreenClip()
        {
            MainWindowViewModel.InvokeMsScreenClip();
            return ValueTask.CompletedTask;
        }
        public ValueTask HideWindow()
        {
            MainWindowViewModel.HideWindow();
            return ValueTask.CompletedTask;
        }
        public ValueTask ShowWindow()
        {
            MainWindowViewModel.ShowWindow();
            return ValueTask.CompletedTask;
        }
    }
}
