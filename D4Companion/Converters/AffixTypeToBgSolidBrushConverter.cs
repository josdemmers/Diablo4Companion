using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;

namespace D4Companion.Converters
{
    public class AffixTypeToBgSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((ItemAffix)value).IsImplicit || ((ItemAffix)value).IsTempered ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
