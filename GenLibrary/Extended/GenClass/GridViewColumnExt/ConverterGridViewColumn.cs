using System;
using System.Windows.Data;
using System.Windows.Controls;
using System.Globalization;

namespace GenLibrary.GenClass
{
    public abstract class ConverterGridViewColumn : GridViewColumn, IValueConverter
    {

        // ----------------------------------------------------------------------
        protected ConverterGridViewColumn(Type bindingType)
        {
            if (bindingType == null)
            {
                throw new ArgumentNullException("bindingType");
            }

            this.bindingType = bindingType;

            // binding
            Binding binding = new Binding();
            binding.Mode = BindingMode.OneWay;
            binding.Converter = this;
            DisplayMemberBinding = binding;
        } // ConverterGridViewColumn

        // ----------------------------------------------------------------------
        public Type BindingType
        {
            get { return bindingType; }
        } // BindingType

        // ----------------------------------------------------------------------
        protected abstract object ConvertValue(object value);

        // ----------------------------------------------------------------------
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!bindingType.IsInstanceOfType(value))
            {
                throw new InvalidOperationException();
            }
            return ConvertValue(value);
        } // IValueConverter.Convert

        // ----------------------------------------------------------------------
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        } // IValueConverter.ConvertBack

        // ----------------------------------------------------------------------
        // members
        private readonly Type bindingType;

    } // class ConverterGridViewColumn

}//
