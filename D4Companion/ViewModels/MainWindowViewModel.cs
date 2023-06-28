using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Reflection;

namespace D4Companion.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IOverlayHandler _overlayHandler;
        private readonly IScreenCaptureHandler _screenCaptureHandler;
        private readonly IScreenProcessHandler _screenProcessHandler;

        private string _windowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version}";

        // Start of Constructors region

        #region Constructors

        public MainWindowViewModel(IEventAggregator eventAggregator, ILogger<MainWindowViewModel> logger,
            IOverlayHandler overlayHandler, IScreenCaptureHandler screenCaptureHandler, IScreenProcessHandler screenProcessHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _overlayHandler = overlayHandler;
            _screenCaptureHandler = screenCaptureHandler;
            _screenProcessHandler = screenProcessHandler;

            // Init View commands
            ApplicationLoadedCmd = new DelegateCommand(ApplicationLoaded);
            LaunchGitHubCommand = new DelegateCommand(LaunchGitHubExecute);
            LaunchKofiCommand = new DelegateCommand(LaunchKofiExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand ApplicationLoadedCmd { get; }
        public DelegateCommand LaunchGitHubCommand { get; }
        public DelegateCommand LaunchKofiCommand { get; }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ApplicationLoaded()
        {
            _logger.LogInformation(WindowTitle);

            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Publish();
        }

        private void LaunchGitHubExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/josdemmers/Diablo4Companion") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void LaunchKofiExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://ko-fi.com/josdemmers") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
