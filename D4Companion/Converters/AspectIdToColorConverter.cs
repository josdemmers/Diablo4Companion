using D4Companion.Interfaces;
using D4Companion.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace D4Companion.Converters
{
    [ValueConversion(typeof(string), typeof(SolidColorBrush))]
    public class AspectIdToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));

            var isMappingReady = systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals((string)value));
            return isMappingReady ?
                new SolidColorBrush(Color.FromRgb(System.Drawing.Color.LightGray.R, System.Drawing.Color.LightGray.G, System.Drawing.Color.LightGray.B)) :
                new SolidColorBrush(Color.FromRgb(System.Drawing.Color.Red.R, System.Drawing.Color.Red.G, System.Drawing.Color.Red.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}