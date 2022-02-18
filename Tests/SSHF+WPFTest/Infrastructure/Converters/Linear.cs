using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSHF.Infrastructure.Converters
{/// <summary>Реализация линейног опреобразованя f(x) = k * x + b </summary>
    internal class Linear: ConverterBase
    {
        public double K
        {
            get;
            set;
        } = 1;
        public double B
        {
            get;
            set;
        }


        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is null) return null;

            double x = System.Convert.ToDouble(value, culture);
            return K * x + B;
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;

            double y = System.Convert.ToDouble(value, culture);

            return (y - B) / K;        
        }
    }
}
