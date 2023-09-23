using D4Companion.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class SigilIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            return affixManager.GetSigilName((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}