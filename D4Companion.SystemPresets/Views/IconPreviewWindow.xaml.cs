using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Messages;
using D4Companion.SystemPresets.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace D4Companion.SystemPresets.Views
{
    /// <summary>
    /// Interaction logic for IconPreviewWindow.xaml
    /// </summary>
    public partial class IconPreviewWindow : Window
    {
        public IconPreviewWindow()
        {
            InitializeComponent();

            // Init messages
            WeakReferenceMessenger.Default.Register<ApplicationClosingMessage>(this, HandleApplicationClosingMessage);

            Topmost = true;
            ShowInTaskbar = false;            
        }

        private void HandleApplicationClosingMessage(object recipient, ApplicationClosingMessage message)
        {
            Close();
        }
    }
}
