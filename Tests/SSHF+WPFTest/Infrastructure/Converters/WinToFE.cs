using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHF.Infrastructure.Converters
{
    internal class WinToFE : ConverterBase
    {
        public override object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return DependencyProperty.UnsetValue;
            if(value is not Window window) return DependencyProperty.UnsetValue;
            return (FrameworkElement)window;
        }
    }
}
