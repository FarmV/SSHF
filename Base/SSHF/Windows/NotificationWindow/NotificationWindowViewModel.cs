using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

using ControlzEx.Standard;

using ReactiveUI;

namespace FVH.SSHF.NotificationWindowArea
{
    public class NotificationWindowViewModel : ReactiveObject
    {
        private Size _monitorResolution;
        private Grid? _gridContent;
        private Visibility _visibleCondition = Visibility.Hidden;
        public NotificationWindowViewModel()
        {
            if(App.DesignerMode is not true) throw new InvalidOperationException("Empty class constructor for designer only");

            MonitorResolution = GetCurrentResolution();
        }
        internal NotificationWindowViewModel(object _) { }              
        private Size GetCurrentResolution()
        {
            Window window = new Window();
            nint handleWindow = new WindowInteropHelper(window).EnsureHandle();
#pragma warning disable CS0618 // Тип или член устарел
            nint intPtr = NativeMethods.MonitorFromWindow(handleWindow, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            MONITORINFO monitorInfoW = NativeMethods.GetMonitorInfoW(intPtr);
#pragma warning restore CS0618 // Тип или член устарел
            Size monitorResolution = new Size(monitorInfoW.rcWork.Width, monitorInfoW.rcWork.Height);
            window.Close();
            return monitorResolution;
        }
        public Size MonitorResolution
        {
            get => _monitorResolution;
            set
            {
                ArgumentOutOfRangeException.ThrowIfZero(value.Height, nameof(MonitorResolution));
                ArgumentOutOfRangeException.ThrowIfZero(value.Width, nameof(MonitorResolution));
                this.RaiseAndSetIfChanged(ref _monitorResolution, value);
            }
        }
        public Grid? Content
        {
            get => _gridContent;
            set
            {
                this.RaiseAndSetIfChanged(ref _gridContent, value);
            }
        }
        public Visibility VisibleCondition
        {
            get => _visibleCondition;
            set => this.RaiseAndSetIfChanged(ref _visibleCondition, value);
        }
    }
}
