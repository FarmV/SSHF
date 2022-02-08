using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SSHF.Infrastructure;
using SSHF.Models.NotifyIconModel;
using SSHF.ViewModels.Base;
using SSHF.Views.Windows.NotifyIcon;

namespace SSHF.ViewModels.NotifyIconViewModel
{
    internal class NotifyIconViewModel : ViewModel
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        

        readonly NotifyIconModel _model;
        public NotifyIconViewModel()
        {          
           _model = new NotifyIconModel(this);

            
        }

        bool _visible = default;    

        public bool MyProperty
        {
            get { return _visible; }
            set { _visible = value; }
        }


        // public ICommand CheckOutside => new RelayCommand(_model.CheckClickOutsideExecute, _model.IsExecuteCheckClickOutside);


        public ICommand ShutdownAppCommand => new RelayCommand(_model.ShutdownAppExecute, _model.IsExecuteShutdownApp);



        //public static readonly RoutedEvent _eventMouse = EventManager.RegisterRoutedEvent("myOutside1", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NotifyIconModel));
        //public static void AddPreviewMouseDownOutsideCapturedElementHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //    UIElement uie = d as UIElement;
        //    if (uie != null)
        //    {
        //        uie.AddHandler(_eventMouse, handler);
        //    }
        //}
        //public static void RemovePreviewMouseDownOutsideCapturedElementHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //    UIElement uie = d as UIElement;
        //    if (uie != null)
        //    {
        //        uie.RemoveHandler(_eventMouse, handler);
        //    }
        //}


    }
}
