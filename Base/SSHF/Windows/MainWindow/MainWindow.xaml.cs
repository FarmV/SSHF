using System;
using System.Windows;

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
