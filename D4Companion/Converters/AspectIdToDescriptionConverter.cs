using D4Companion.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class AspectIdToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            string result = affixManager.GetAspectDescription((string)value);
            return string.IsNullOrWhiteSpace(result) ? affixManager.GetUniqueDescription((string)value) : affixManager.GetAspectDescription((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
