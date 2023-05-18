using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

using ReactiveUI;

using SSHF.ViewModels.MainWindowViewModel;

namespace SSHF
{



    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow, IViewFor<MainWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow));
        public MainWindow()
        {

            InitializeComponent();
            this.Title = "Fast Window";



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
