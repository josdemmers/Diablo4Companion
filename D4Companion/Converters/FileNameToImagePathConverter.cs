using D4Companion.Events;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace D4Companion.Converters
{
    public class FileNameToImagePathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string fileName = (string)values[0];
                string category = (string)values[1];
                string systemPreset = (string)values[2];

                return new BitmapImage(new Uri($"{Environment.CurrentDirectory}/Images/{systemPreset}/{category}/{fileName}"));
            }
            catch (Exception)
            {
                var eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
                eventAggregator.GetEvent<ExceptionOccurredEvent>().Publish(new ExceptionOccurredEventParams
                {
                    Message = $"File not found: ./Images/{(string)values[2]}/{(string)values[1]}/{(string)values[0]}"
                });
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
