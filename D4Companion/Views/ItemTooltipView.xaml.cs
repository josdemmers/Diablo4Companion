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

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for ItemTooltipView.xaml
    /// </summary>
    public partial class ItemTooltipView : UserControl
    {
        public ItemTooltipView()
        {
            InitializeComponent();
        }

        private void TextBoxFilter_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxFilterWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxFilter_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxFilter.Text))
            {
                TextBoxFilterWatermark.Visibility = Visibility.Visible;
            }
        }
    }
}
