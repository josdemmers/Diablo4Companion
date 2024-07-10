using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(Brush))]
    public class SystemPresetStatusToHealthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string valueAsString = (string)value;
            if (string.IsNullOrWhiteSpace(valueAsString))
            {
                return Brushes.Gray;
            }

            switch (valueAsString.ToLower())
            {
                case Constants.SystemPresetStatusConstants.Broken:
                    return Brushes.Red;
                case Constants.SystemPresetStatusConstants.Ready:
                    return Brushes.Green;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
