﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SSHF.Infrastructure.Converters
{
    internal abstract class ConverterBase: IValueConverter
    {
        public abstract object? Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public virtual object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException("Обратное преобразование не поддерживается");

    }
}
