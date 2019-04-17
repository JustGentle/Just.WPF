using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Reflection;

namespace GenLibrary.GenControls
{
    /// <summary>
    /// 用于Treelistview和ListView的垂直线偏移转换，此线受DPI放大的影响
    /// </summary>
    public class LevelToVerLnMarginForTreeList : IValueConverter
    {
        public object Convert(object o, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            Double indentSize = 0;
            if (parameter != null)
                Double.TryParse(parameter.ToString(), out indentSize);
            return new Thickness(((int)o) * indentSize+9, 24, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }//
}//
