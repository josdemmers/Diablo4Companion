using D4Companion.Interfaces;
using D4Companion.Services;
using D4Companion.Views;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace D4Companion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private static Mutex? _mutex = null;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

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

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindow>();
            return w;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services
            containerRegistry.RegisterSingleton<IAffixPresetManager, AffixPresetManager>();
            containerRegistry.RegisterSingleton<IHttpClientHandler, HttpClientHandler>();
            containerRegistry.RegisterSingleton<IOverlayHandler, OverlayHandler>();
            containerRegistry.RegisterSingleton<IReleaseManager, ReleaseManager>();
            containerRegistry.RegisterSingleton<ISettingsManager, SettingsManager>();
            containerRegistry.RegisterSingleton<IScreenCaptureHandler, ScreenCaptureHandler>();
            containerRegistry.RegisterSingleton<IScreenProcessHandler, ScreenProcessHandler>();
            containerRegistry.RegisterSingleton<ISystemPresetManager, SystemPresetManager>();

            // Register Metro
            containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
                loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog.config"));

            return new DryIocContainerExtension(new Container(CreateContainerRules()).WithDependencyInjectionAdapter(serviceCollection));
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
    }
}
