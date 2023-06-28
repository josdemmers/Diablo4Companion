using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace D4Companion.Converters
{
    public class FileNameToImagePathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = (string)values[0];
            string category = (string)values[1];
            string systemPreset = (string)values[2];

            return new BitmapImage(new Uri($"{Environment.CurrentDirectory}/Images/{systemPreset}/{category}/{fileName}"));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
