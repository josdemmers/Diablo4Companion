using D4Companion.ViewModels.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System;

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

        private void TextBoxBuildIdD2Core_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBuildIdD2CoreWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxBuildIdD2Core_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBuildIdD2Core.Text))
            {
                TextBoxBuildIdD2CoreWatermark.Visibility = Visibility.Visible;
            }
        }

        private void TextBoxBuildIdD4Builds_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBuildIdD4BuildsWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxBuildIdD4Builds_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBuildIdD4Builds.Text))
            {
                TextBoxBuildIdD4BuildsWatermark.Visibility = Visibility.Visible;
            }
        }

        private void TextBoxBuildIdMobalytics_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxBuildIdMobalyticsWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxBuildIdMobalytics_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBuildIdMobalytics.Text))
            {
                TextBoxBuildIdMobalyticsWatermark.Visibility = Visibility.Visible;
            }
        }

        private void TextBoxBuildIdMobalytics_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBoxBuildIdMobalytics.Text))
            {
                TextBoxBuildIdMobalyticsWatermark.Visibility = Visibility.Collapsed;
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

            // Dispose VM to unregister message handlers
            (DataContext as IDisposable)?.Dispose();
        }
    }
}
