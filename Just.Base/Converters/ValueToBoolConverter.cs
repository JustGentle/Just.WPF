﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Just.Base.Converters
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class ValueToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
