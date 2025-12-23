using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for TradeView.xaml
    /// </summary>
    public partial class TradeView : UserControl
    {
        public TradeView()
        {
            DataContext = App.Current.Services.GetRequiredService<TradeViewModel>();

            InitializeComponent();
        }
    }
}
