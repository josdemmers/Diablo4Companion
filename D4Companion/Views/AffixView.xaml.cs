using D4Companion.ViewModels;
using Microsoft.Win32;
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
    }
}
