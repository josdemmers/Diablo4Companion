using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            DataContext = App.Current.Services.GetRequiredService<DashboardViewModel>();

            InitializeComponent();
        }

        private void ScreenshotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            if (ScreenshotImage.Source is not BitmapSource bitmapSource) return;

            // Click position in WPF DIPs
            Point click = e.GetPosition(ScreenshotImage);

            // Scale DIP to pixel
            double scaleX = bitmapSource.PixelWidth / ScreenshotImage.ActualWidth;
            double scaleY = bitmapSource.PixelHeight / ScreenshotImage.ActualHeight;

            int pixelX = (int)(click.X * scaleX);
            int pixelY = (int)(click.Y * scaleY);

            pixelX = Math.Clamp(pixelX, 0, bitmapSource.PixelWidth - 1);
            pixelY = Math.Clamp(pixelY, 0, bitmapSource.PixelHeight - 1);

            var viewmodel = ((DashboardViewModel)DataContext);
            if (viewmodel.SelectedThumbnailEdit == null) return;

            if (viewmodel.IsSourceROIActive)
            {
                viewmodel.SelectedThumbnailEdit.SourcePositionX = pixelX;
                viewmodel.SelectedThumbnailEdit.SourcePositionY = pixelY;
            }
            else
            {
                viewmodel.SelectedThumbnailEdit.DestinationPositionX = pixelX;
                viewmodel.SelectedThumbnailEdit.DestinationPositionY = pixelY;
            }
        }

        private void TextBoxThumbnail_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxThumbnailWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxThumbnail_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxThumbnail.Text))
            {
                TextBoxThumbnailWatermark.Visibility = Visibility.Visible;
            }
        }        
    }
}
