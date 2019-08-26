using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Just.Base.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class ValueToVisibilityConverter : IValueConverter
    {
        public ValueToVisibilityConverter()
            : this(true)
        {

        }
        public ValueToVisibilityConverter(bool collapsewhenInvisible)
            : base()
        {
            CollapseWhenInvisible = collapsewhenInvisible;
        }
        public bool CollapseWhenInvisible { get; set; }

        public Visibility FalseVisible
        {
            get
            {
                if (CollapseWhenInvisible)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }

        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FalseVisible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
