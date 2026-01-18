using D4Companion.Interfaces;
using D4Companion.Services;
using D4Companion.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace D4Companion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex = null;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

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

        // Start of Event handlers region

        #region Event handlers

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "D4Companion";

            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Application.Current.Shutdown();
            }

            base.OnStartup(e);

            SetupExceptionHandling();
        }

        #endregion

        // Start of Methods region

        #region Methods

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(loggingBuilder => loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog.config"));

            // Services
            services.AddSingleton<IAffixManager, AffixManager>();
            services.AddSingleton<IBuildsManagerD2Core, BuildsManagerD2Core>();
            services.AddSingleton<IBuildsManagerD4Builds, BuildsManagerD4Builds>();
            services.AddSingleton<IBuildsManagerMaxroll, BuildsManagerMaxroll>();
            services.AddSingleton<IBuildsManagerMobalytics, BuildsManagerMobalytics>();
            services.AddSingleton<IDialogCoordinator, DialogCoordinator>();
            services.AddSingleton<IHttpClientHandler, HttpClientHandler>();
            services.AddSingleton<IOcrHandler, OcrHandler>();
            services.AddSingleton<IOverlayHandler, OverlayHandler>();
            services.AddSingleton<IReleaseManager, ReleaseManager>();
            services.AddSingleton<ISettingsManager, SettingsManager>();
            services.AddSingleton<IScreenCaptureHandler, ScreenCaptureHandler>();
            services.AddSingleton<IScreenProcessHandler, ScreenProcessHandler>();
            services.AddSingleton<ISystemPresetManager, SystemPresetManager>();
            services.AddSingleton<ITradeItemManager, TradeItemManager>();

            // ViewModels
            services.AddTransient<AffixViewModel>();
            services.AddTransient<DebugViewModel>();
            services.AddTransient<LoggingViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<TradeViewModel>();

            return services.BuildServiceProvider();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                _logger.Error(exception, message);
            }
        }

        #endregion
    }
}
