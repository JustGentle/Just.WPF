using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Reflection;

namespace GenLibrary.GenClass
{
    /// <summary>
    /// 用于Treelistview和ListView的垂直线偏移转换，此线受DPI放大的影响
    /// </summary>
    public class ListViewVerticalLineMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double thickValue = 0;
            if (value != null)
                thickValue =System.Convert.ToDouble(value);
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            //获取x轴的DPI
            int DpiX = (int)dpiXProperty.GetValue(null, null);
            double StandartDpiX = 96.0;
            double cov = DpiX* thickValue / StandartDpiX;
            return new Thickness(cov,0,0,0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }//
}//
