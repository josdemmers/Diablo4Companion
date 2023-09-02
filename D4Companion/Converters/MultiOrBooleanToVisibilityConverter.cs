using System;
using System.Globalization;
using System.Windows.Data;

namespace D4Companion.Converters
{
    public class MultiOrBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = false;
            foreach (object value in values)
            {
                if (value is bool)
                {
                    visible = visible || (bool)value;
                }
            }

            if (visible)
            {
                return System.Windows.Visibility.Visible;
            }
            else
            {
                return System.Windows.Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
