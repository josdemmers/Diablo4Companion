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

        public object Convert(object aValue, Type aTargetType, object aParameter, System.Globalization.CultureInfo aCultureInfo)
        {
            return aValue != null && ((bool)aValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object aValue, Type aTargetType, object aParameter, System.Globalization.CultureInfo aCultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}