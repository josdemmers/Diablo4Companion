using D4Companion.Events;
using Prism.Events;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace D4Companion.Converters
{
    public class FlagToImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string flag = (string)value;

                var uri = new Uri($"pack://application:,,,/Images/Flags/{flag}.png");
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = uri;
                bitmap.EndInit();

                return bitmap;
            }
            catch (Exception)
            {
                var eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
                eventAggregator.GetEvent<ExceptionOccurredEvent>().Publish(new ExceptionOccurredEventParams
                {
                    Message = $"File not found: ./Images/Flags/{(string)value}.png"
                });
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
