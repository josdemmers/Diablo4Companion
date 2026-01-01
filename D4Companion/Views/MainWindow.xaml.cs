using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            // Only set DataContext when not in Design-mode
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = App.Current.Services.GetRequiredService<MainWindowViewModel>();
            }

            InitializeComponent();
        }

        private void HamburgerMenuControl_ItemInvoked(object sender, MahApps.Metro.Controls.HamburgerMenuItemInvokedEventArgs args)
        {
            this.HamburgerMenuControl.Content = args.InvokedItem;
        }
    }
}
