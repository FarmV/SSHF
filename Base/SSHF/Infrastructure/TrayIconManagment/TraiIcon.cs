using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SSHF.Infrastructure.TrayIconManagment
{
    internal class TraiIcon : IDisposable
    {
        private bool _disposed;
        private bool _blockRepeatInvokeMessageBox = false;
        private readonly DPIIconHandler _dpiCorrector;
        private NotifyIcon _taskbarIcon;
        public TraiIcon(Stream resourceIcon, int[]? sizesIcon = null)
        {
            _dpiCorrector = new DPIIconHandler(resourceIcon, sizesIcon);
            _taskbarIcon = new NotifyIcon
            {
                Icon = _dpiCorrector.GetDefaultStartProccesIconDPI(),
                Visible = true
            };
            _dpiCorrector.ActualSizeIcon += ActualSizeIconLogic;
            _taskbarIcon.MouseDown += TaskbarIcon_MouseDown;          
        }
        private void TaskbarIcon_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_blockRepeatInvokeMessageBox is true) return;
            _blockRepeatInvokeMessageBox = true;
            if (System.Windows.MessageBox.Show("Закрыть приложение?", "Запрос SSHF", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) is MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => { System.Windows.Application.Current.Shutdown(); });
                return;
            }
            _blockRepeatInvokeMessageBox = false;
        }    
        public void Dispose()
        {
            if (_disposed is true) return;
            _dpiCorrector.Dispose();
            _taskbarIcon.Dispose();
            _disposed = true;
        }
        private void ActualSizeIconLogic(object? _, System.Drawing.Icon newSizeIcon)
        {
            _taskbarIcon.MouseDown -= TaskbarIcon_MouseDown;
            _taskbarIcon.Visible = false;
            _taskbarIcon.Dispose();
            Thread.Sleep(450); // NotifyIcon.Dispose Возвращает управление раньше чем фактически освободит ресурысы.
            _taskbarIcon = new NotifyIcon
            {
                Icon = newSizeIcon,
                Visible = true
            };
            _taskbarIcon.MouseDown += TaskbarIcon_MouseDown;
        }
    }
}
