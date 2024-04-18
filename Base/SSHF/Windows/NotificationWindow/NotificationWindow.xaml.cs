using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using ReactiveUI;

namespace FVH.SSHF.NotificationWindowArea
{
    public partial class NotificationWindow : MahApps.Metro.Controls.MetroWindow, IViewFor<NotificationWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(NotificationWindowViewModel), typeof(NotificationWindow));
        private const long WS_EX_TOOLWINDOW = 0x00000080;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private const int GWL_EXSTYLE = -20;

        public NotificationWindow()
        {
            InitializeComponent();
            this.Title = "Notification Window";

            HideAltTabWindow();
        }
        private void HideAltTabWindow()
        {
            nint handleWindow = new WindowInteropHelper(this).EnsureHandle();
            NativeHelper.SetWindowLongPtrW(handleWindow, GWL_EXSTYLE, new nint(NativeHelper.GetWindowLongPtrW(handleWindow, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
        }
        /// <summary>
        /// Заглушка - Изменения свойства Visibility деактивирует привязку к размерам окна.
        /// Решение установить привязку после изменение Visibility и не изменять это свойство.Реализовать сокрытие окна через opacity.
        /// </summary>
        //private void SetBindingSizePostSwitchVisible() // todo Вроде нужно освободить ресурсы привязок чтобы объект мог быть собран сборщиком мусора
        //{
        //    this.OneWayBind(
        //         this.ViewModel,
        //         vm => vm.Height,
        //         w => w.GridContent.Height);
        //    this.OneWayBind(
        //         this.ViewModel,
        //         vm => vm.Width,
        //         w => w.GridContent.Width);

        //    Не понятно нужно ли биндить размеры самого окна. При SizeToContent = WidthAndHeight размер окна фактически больше на пару пикселей чем целевой Gird(Структура наследования фактически отличается в "MahApps.Metro.Controls.MetroWindow", ежели это было бы прямое наследование от "System.Windows.Window").
        //    this.OneWayBind(
        //         this.ViewModel,
        //         vm => vm.Height,
        //         w => w.Height);
        //    this.OneWayBind(
        //         this.ViewModel,
        //         vm => vm.Width,
        //         w => w.Width);
        //}
        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set
            {
                if (value is not NotificationWindowViewModel vm) throw new InvalidOperationException($"ViewModel is not {nameof(NotificationWindowViewModel)}");
                ViewModel = vm;
                SetBindingSizePostSwitchVisible();
            }
        }
        public NotificationWindowViewModel? ViewModel
        {
            get => (NotificationWindowViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
        private static partial class NativeHelper
        {
            [DllImport("user32")]
            internal static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);
            [DllImport("user32")]
            internal static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);
        }
    }
}
