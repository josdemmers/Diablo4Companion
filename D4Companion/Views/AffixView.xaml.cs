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

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as AffixViewModel;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                viewModel?.ImportAffixPresetCommandExecute(openFileDialog.FileName);
            }
        }
    }
}
