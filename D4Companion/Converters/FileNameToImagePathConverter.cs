using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Messages;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace D4Companion.Converters
{
    public class FileNameToImagePathConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string fileName = (string)values[0];
                string folder = (string)values[1];
                string systemPreset = (string)values[2];

                var uri = new Uri($"{Environment.CurrentDirectory}/Images/{systemPreset}/{folder}/{fileName}");
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
                WeakReferenceMessenger.Default.Send(new ExceptionOccurredMessage(new ExceptionOccurredMessageParams
                {
                    Message = $"File not found: ./Images/{(string)values[2]}/{(string)values[1]}/{(string)values[0]}"
                }));
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
