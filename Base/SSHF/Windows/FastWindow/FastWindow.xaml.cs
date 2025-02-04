

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using R3;


namespace FVH.SSHF.FastWindowArea
{

    public partial class FastWindow : MahApps.Metro.Controls.MetroWindow 
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(FastWindowViewModel), typeof(FastWindow));
        private readonly int GWL_EXSTYLE = -20;
        private readonly long WS_EX_TOOLWINDOW = 0x00000080;
        private readonly long WS_EX_NOACTIVATE = 0x08000000L;
        private IDisposable? _bind;
        public FastWindow()
        {
            InitializeComponent();
            this.Title = "Fast Window";

  
            HideAltTabWindow();

#if OneFastWindowNotTopMost
            this.Topmost = false;
#endif
        }
        private void HideAltTabWindow()
        {
            IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
            NativeHelper.SetWindowLongPtrW(hWnd, GWL_EXSTYLE, new IntPtr(NativeHelper.GetWindowLongPtrW(hWnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _bind?.Dispose();
        }
        /// <summary>
        /// Заглушка - Изменения свойства Visibility деактивирует привязку к размерам окна.
        /// Решение установить привязку после изменение Visibility и не изменять это свойство. Реализовать сокрытие окна через opacity.
        /// </summary>
        private void SetBindingSizePostSwitchVisible()
        {
            IDisposable d1 = this.ViewModel!.Height.Subscribe(onNext: newHeight =>
            {
                GridContent.Height = newHeight;
            });
            IDisposable d2 = this.ViewModel!.Width.Subscribe(onNext: newHeight => GridContent.Width = newHeight);
            IDisposable d3 = this.ViewModel!.Height.Subscribe(onNext: newHeight => this.Height = newHeight);
            IDisposable d4 = this.ViewModel!.Width.Subscribe(onNext: newHeight => this.Width = newHeight);

            _bind = R3.Disposable.Combine(d1, d2, d3, d4);
        }
        public FastWindowViewModel? ViewModel
        {
            get => (FastWindowViewModel)GetValue(ViewModelProperty);
            set
            {
                SetValue(ViewModelProperty, value);
                if(_bind is not null)
                {
                    _bind.Dispose();
                    _bind = null;
                }
                SetBindingSizePostSwitchVisible();
            }
        }
        private static partial class NativeHelper
        {
            [LibraryImport("user32")]
            [return:MarshalAs(UnmanagedType.SysInt)]
            internal static partial nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);
            [LibraryImport("user32")]
            [return: MarshalAs(UnmanagedType.SysInt)]
            internal static partial nint GetWindowLongPtrW(nint hWnd, int nIndex);
        }
    }
}
