using D4Companion.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class AspectIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var affixManager = App.Current.Services.GetRequiredService<IAffixManager>();
            return affixManager.GetAspectName((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
