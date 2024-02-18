using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FVH.SSHF.Infrastructure.Converters
{
    class ConverterVisibleToOpacity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility visibility) throw new InvalidCastException();
            if (visibility is Visibility.Visible) return 100;
            else if (visibility is Visibility.Hidden) return 0;
            else
            {
                throw new InvalidCastException();
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is 100) return Visibility.Visible;
            else if (value is 0) return Visibility.Hidden;
            else
            {
                throw new InvalidCastException();
            }
        }
    }
}
