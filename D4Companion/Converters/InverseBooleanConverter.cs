using System;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        static InverseBooleanConverter()
        {
            Instance = new InverseBooleanConverter();
        }

        public static InverseBooleanConverter Instance { get; private set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureInfo)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}