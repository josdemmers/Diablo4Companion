using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace D4Companion.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SetAspectMappingView.xaml
    /// </summary>
    public partial class SetAspectMappingView : UserControl
    {
        public SetAspectMappingView()
        {
            InitializeComponent();
        }

        private async void ButtonDone_Click(object sender, RoutedEventArgs e)
        {
            var dialog = (sender as DependencyObject).TryFindParent<BaseMetroDialog>();
            await (Application.Current.MainWindow as MetroWindow).HideMetroDialogAsync(dialog);
        }

        private void TextBoxFilterAspect_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxFilterAspectWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxFilterAspect_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxFilterAspect.Text))
            {
                TextBoxFilterAspectWatermark.Visibility = Visibility.Visible;
            }
        }
    }
}
