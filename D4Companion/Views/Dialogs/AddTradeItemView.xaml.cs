using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace D4Companion.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTradeItemView.xaml
    /// </summary>
    public partial class AddTradeItemView : UserControl
    {
        public AddTradeItemView()
        {
            InitializeComponent();
        }

        private async void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = (sender as DependencyObject).TryFindParent<BaseMetroDialog>();
            await (Application.Current.MainWindow as MetroWindow).HideMetroDialogAsync(dialog);
        }

        private async void ButtonDone_Click(object sender, RoutedEventArgs e)
        {
            var dialog = (sender as DependencyObject).TryFindParent<BaseMetroDialog>();
            await (Application.Current.MainWindow as MetroWindow).HideMetroDialogAsync(dialog);
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
