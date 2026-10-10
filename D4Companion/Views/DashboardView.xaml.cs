using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
