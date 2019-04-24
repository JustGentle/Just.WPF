using System;
using System.Globalization;
using System.Windows.Data;

namespace Just.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            return !(bool)value;
        }
    }
}
