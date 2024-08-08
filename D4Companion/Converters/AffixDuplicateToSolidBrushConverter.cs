using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;

namespace D4Companion.Converters
{
    public class AffixDuplicateToSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isDuplicate = (bool)value;
            return isDuplicate ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
