using D4Companion.ViewModels.Entities;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.Converters
{
    public class ThumbnailReferenceEqualsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;
            if (values[1] == null)
                return false;
            if (values[1] == DependencyProperty.UnsetValue)
                return false;

            return ReferenceEquals(values[0], ((ThumbnailPersistentVM)values[1]).Model);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}