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
