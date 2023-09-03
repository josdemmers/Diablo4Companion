using D4Companion.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class MappingReadyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string idName = (string)value;
            if (string.IsNullOrWhiteSpace(idName)) return Visibility.Visible;

            var systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));
            return systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(idName)) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
