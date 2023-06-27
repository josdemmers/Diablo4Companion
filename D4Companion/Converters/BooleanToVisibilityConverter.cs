using System;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        static BooleanToVisibilityConverter()
        {
            Instance = new BooleanToVisibilityConverter();
        }

        public static BooleanToVisibilityConverter Instance { get; private set; }

        public object Convert(object aValue, Type aTargetType, object aParameter, System.Globalization.CultureInfo aCultureInfo)
        {
            return aValue != null && ((bool)aValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object aValue, Type aTargetType, object aParameter, System.Globalization.CultureInfo aCultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}