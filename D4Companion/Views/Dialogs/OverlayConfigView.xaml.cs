using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace D4Companion.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for OverlayConfigView.xaml
    /// </summary>
    public partial class OverlayConfigView : UserControl
    {
        public OverlayConfigView()
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
