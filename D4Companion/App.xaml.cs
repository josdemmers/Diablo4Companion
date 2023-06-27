using D4Companion.Interfaces;
using D4Companion.Services;
using D4Companion.Views;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using System.Threading;
using System.Windows;

namespace D4Companion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private static Mutex? _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "D4Helper";

            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Application.Current.Shutdown();
            }

            base.OnStartup(e);
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
            containerRegistry.RegisterSingleton<IOverlayHandler, OverlayHandler>();
            containerRegistry.RegisterSingleton<ISettingsManager, SettingsManager>();
            containerRegistry.RegisterSingleton<IScreenCaptureHandler, ScreenCaptureHandler>();
            containerRegistry.RegisterSingleton<IScreenProcessHandler, ScreenProcessHandler>();
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
                loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog.config"));

            return new DryIocContainerExtension(new Container(CreateContainerRules()).WithDependencyInjectionAdapter(serviceCollection));
        }
    }
}
