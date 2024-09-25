using System;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        static InverseBooleanToVisibilityConverter()
        {
            Instance = new InverseBooleanToVisibilityConverter();
        }

        public static InverseBooleanToVisibilityConverter Instance { get; private set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureInfo)
        {
            return value != null && ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}