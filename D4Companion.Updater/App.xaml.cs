using D4Companion.Interfaces;
using D4Companion.Services;
using D4Companion.Updater.Views;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace D4Companion.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindow>();
            return w;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services
            containerRegistry.RegisterSingleton<IHttpClientHandler, HttpClientHandler>();
            containerRegistry.RegisterSingleton<IReleaseManager, ReleaseManager>();
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
                loggingBuilder.AddNLog(configFileRelativePath: "Config/NLog-updater.config"));

            return new DryIocContainerExtension(new Container(CreateContainerRules()).WithDependencyInjectionAdapter(serviceCollection));
        }
    }
}
