using D4Companion.Updater.Interfaces;
using D4Companion.Updater.Services;
using D4Companion.Updater.ViewModels;
using D4Companion.Updater.Views;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Windows;

namespace D4Companion.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Start of Constructors region

        #region Constructors

        public App()
        {
            InitializeComponent();

            Services = ConfigureServices();
        }

        #endregion

        // Start of Properties region

        #region Properties

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        #endregion

        // Start of Methods region

        #region Methods

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(loggingBuilder => loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog-updater.config"));

            // Services
            services.AddSingleton<IDownloadManager, DownloadManager>();
            services.AddSingleton<IHttpClientHandler, HttpClientHandler>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            return services.BuildServiceProvider();
        }

        #endregion
    }
}
