using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using FVH.SSHF.NotificationWindowArea;

namespace FVH.SSHF
{
    internal class DesktopNotification : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly NotificationWindow _notificationWindow;
        private System.Windows.Threading.DispatcherTimer? _timer;
        internal bool IsDisposed = false;
        internal DesktopNotification(Dispatcher uiDispatcher)
        {
            _dispatcher = uiDispatcher;
            _timer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Normal, uiDispatcher);
            _notificationWindow = new NotificationWindow { DataContext = new NotificationWindowViewModel() };
            _notificationWindow.Show();
        }
        public void Dispose()
        {
            if(IsDisposed is true) return;
            _dispatcher?.Invoke(() => { _notificationWindow.Close(); });
            IsDisposed = true;
        }
        internal Task NotificationAsync(TimeSpan? timeSpan, Grid gridContent)
        {
            timeSpan ??= TimeSpan.FromSeconds(2);

            NotificationWindowViewModel? viewModel = null;
            void ShowContent()
            {
                viewModel = (NotificationWindowViewModel)_notificationWindow.DataContext;
                viewModel.Content = gridContent;
            }

            _timer = new System.Windows.Threading.DispatcherTimer(timeSpan.Value, DispatcherPriority.Normal, 
            (object? _, EventArgs e) => 
            {
                _timer!.Stop();
                _timer = null;
                viewModel!.VisibleCondition = Visibility.Hidden;
                viewModel.Content = null;
            }, 
            _dispatcher);
            _dispatcher.Invoke(ShowContent);
            _timer.Start();

            return Task.CompletedTask;
        }
    }
}
