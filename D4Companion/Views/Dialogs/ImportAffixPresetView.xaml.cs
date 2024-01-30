using D4Companion.ViewModels.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace D4Companion.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ImportAffixPresetView.xaml
    /// </summary>
    public partial class ImportAffixPresetView : UserControl
    {
        public ImportAffixPresetView()
        {
            InitializeComponent();
        }

        private void TextBoxBuildId_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBuildIdWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxBuildId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBuildId.Text))
            {
                TextBoxBuildIdWatermark.Visibility = Visibility.Visible;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as ImportAffixPresetViewModel;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                viewModel?.ImportAffixPresetCommandExecute(openFileDialog.FileName);
            }
        }

        private async void ButtonDone_Click(object sender, RoutedEventArgs e)
        {
            var dialog = (sender as DependencyObject).TryFindParent<BaseMetroDialog>();
            await (Application.Current.MainWindow as MetroWindow).HideMetroDialogAsync(dialog);
        }
    }
}
