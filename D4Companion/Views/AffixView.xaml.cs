using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for AffixView.xaml
    /// </summary>
    public partial class AffixView : UserControl
    {
        public AffixView()
        {
            DataContext = App.Current.Services.GetRequiredService<AffixViewModel>();

            InitializeComponent();
        }

        private void TextBoxFilterAffix_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxFilterAffixWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxFilterAffix_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxFilterAffix.Text))
            {
                TextBoxFilterAffixWatermark.Visibility = Visibility.Visible;
            }
        }

        private void TextBoxPresetName_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxPresetNameWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxPresetName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxPresetName.Text))
            {
                TextBoxPresetNameWatermark.Visibility = Visibility.Visible;
            }
        }
    }
}
