using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;


namespace D4Companion.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for InfinityBuildsDownloadView.xaml
    /// </summary>
    public partial class InfinityBuildsDownloadView : UserControl
    {
        public InfinityBuildsDownloadView()
        {
            InitializeComponent();
        }

        private async void ButtonDone_Click(object sender, RoutedEventArgs e)
        {
            var dialog = (sender as DependencyObject).TryFindParent<BaseMetroDialog>();
            await (Application.Current.MainWindow as MetroWindow).HideMetroDialogAsync(dialog);
        }
    }
}
