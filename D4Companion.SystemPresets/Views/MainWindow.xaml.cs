using D4Companion.SystemPresets.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace D4Companion.SystemPresets.Views
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

        private void TextBoxSystemPreset_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxSystemPresetWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxSystemPreset_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxSystemPreset.Text))
            {
                TextBoxSystemPresetWatermark.Visibility = Visibility.Visible;
            }
        }
    }
}