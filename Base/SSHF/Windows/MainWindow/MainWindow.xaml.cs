using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using ControlzEx.Standard;

using ReactiveUI;

using SSHF.ViewModels.MainWindowViewModel;

namespace SSHF
{
    public static partial class NativeHelper
    {
        [LibraryImport("user32")]
        internal static partial IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [LibraryImport("user32")]
        internal static partial IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);
    }
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, IViewFor<MainWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow));
        private readonly int GWL_EXSTYLE = -20;
        private readonly long WS_EX_TOOLWINDOW = 0x00000080;
        private readonly long WS_EX_NOACTIVATE = 0x08000000L;
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Fast Window";

            HideAltTabWindow();
        }
        private void HideAltTabWindow()
        {
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            NativeHelper.SetWindowLongPtrW(hWnd, GWL_EXSTYLE, new IntPtr(NativeHelper.GetWindowLongPtrW(hWnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
        }       
        /// <summary>
        /// Заглушка - Изменения свойста Visibility деактивирует привязку к размерам окна.
        /// Решение устанвоить приязку после изменение Visibility и не изменять это свойсто. Реализовать сокрытие окна через opacity.
        /// </summary>
        private void SetBindingSizePostSwithVisible()
        {
            this.OneWayBind(
                 this.ViewModel,
                 vm => vm.Height,
                 w => w.Height);
            this.OneWayBind(
                 this.ViewModel,
                 vm => vm.Width,
                 w => w.Width);          
        }
        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set
            {
                if (value is not MainWindowViewModel vm) throw new InvalidOperationException($"ViewModel is not {nameof(MainWindowViewModel)}");
                ViewModel = vm;
                SetBindingSizePostSwithVisible();
            }
        }
        public MainWindowViewModel? ViewModel
        {
            get => (MainWindowViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}
