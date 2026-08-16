using D4Companion.SystemPresets.ViewModels.Entities;
using System.Globalization;
using System.Windows.Data;

namespace D4Companion.SystemPresets.Converters
{
    public class ReferenceEqualsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return false;
            if (values[1] == null)
                return false;

            return ReferenceEquals(values[0], ((IconTypeVM)values[1]).Model);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}