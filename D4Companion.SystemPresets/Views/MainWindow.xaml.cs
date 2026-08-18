using D4Companion.SystemPresets.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            // Only set DataContext when not in Design-mode
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = App.Current.Services.GetRequiredService<MainWindowViewModel>();
            }

            InitializeComponent();
        }

        private void TextBoxSystemPreset_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxSystemPresetWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxSystemPreset_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxSystemPreset.Text))
            {
                TextBoxSystemPresetWatermark.Visibility = Visibility.Visible;
            }
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

            var viewmodel = ((MainWindowViewModel)DataContext);
            if (viewmodel.SelectedIconTypeEdit == null) return;

            viewmodel.SelectedIconTypeEdit.PositionX = pixelX;
            viewmodel.SelectedIconTypeEdit.PositionY = pixelY;
        }
    }
}