using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

using FVH.SSHF.FastWindowArea;

using R3;

namespace FVH.SSHF.Infrastructure.TrayIconManagement
{
    internal  class TrayIcon : IDisposable
    {
        private bool _disposed;
        private bool _blockRepeatInvokeMessageBox = false;
        private readonly DPIIconHandler _dpiCorrector;
        private NotifyIcon _taskbarIcon;
        public TrayIcon(Stream resourceIcon, int[]? sizesIcon = null)
        {
            _dpiCorrector = new DPIIconHandler(resourceIcon, sizesIcon);
            _taskbarIcon = new NotifyIcon
            {
                Icon = _dpiCorrector.GetDefaultStartProcessIconDPI(),
                Visible = true
            };
            _dpiCorrector.ActualSizeIcon += ActualSizeIconLogic;
            _taskbarIcon.MouseDown += TaskbarIcon_MouseDown;          
        }
        private void TaskbarIcon_MouseDown(object? sender, MouseEventArgs e) 
        {
            if (_blockRepeatInvokeMessageBox is true) return;
            _blockRepeatInvokeMessageBox = true;
            ((FastWindow)System.Windows.Application.Current.MainWindow).ShowInTaskbar = false;// чтобы получить модальное окно без отображение в панели задач 
            if (System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Закрыть приложение?", "Запрос SSHF", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) is MessageBoxResult.Yes)
            {
                // владельцем обязательно должно быть главное окно, так как нужно перестанавливать стиль
                System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Application.Current.Shutdown);
                return;
            }
            ((FastWindow)System.Windows.Application.Current.MainWindow).ShowInTaskbar = true;
            ((FastWindow)System.Windows.Application.Current.MainWindow).SetStyleWindow(ensureUseStyle:true); // переустановить  стиль главного окна, так как вызов MessageBox.Show переопределяет стиль
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
            Thread.Sleep(450); // NotifyIcon.Dispose() Возвращает управление раньше, чем фактически освободит ресурсы.
            _taskbarIcon = new NotifyIcon
            {
                Icon = newSizeIcon,
                Visible = true
            };
            _taskbarIcon.MouseDown += TaskbarIcon_MouseDown;
        }
    }
}
