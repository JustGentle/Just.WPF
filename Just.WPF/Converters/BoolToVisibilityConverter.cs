using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Just.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter()
            : this(true)
        {

        }
        public BoolToVisibilityConverter(bool collapsewhenInvisible)
            : base()
        {
            CollapseWhenInvisible = collapsewhenInvisible;
        }
        public bool CollapseWhenInvisible { get; set; }
        public bool Reverse { get; set; }

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
                return Reverse ? FalseVisible : Visibility.Visible;
            var trueOrFalse = (bool)value;
            if (Reverse) trueOrFalse = !trueOrFalse;
            return trueOrFalse ? Visibility.Visible : FalseVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return !Reverse;
            var trueOrFalse = ((Visibility)value == Visibility.Visible);
            if (Reverse) trueOrFalse = !trueOrFalse;
            return trueOrFalse;
        }
    }
}
