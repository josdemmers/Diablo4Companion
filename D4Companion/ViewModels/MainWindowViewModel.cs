using D4Companion.Events;
using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace D4Companion.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IOverlayHandler _overlayHandler;
        private readonly IReleaseManager _releaseManager;
        private readonly IScreenCaptureHandler _screenCaptureHandler;
        private readonly IScreenProcessHandler _screenProcessHandler;

        private string _windowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version}";

        // Start of Constructors region

        #region Constructors

        public MainWindowViewModel(IEventAggregator eventAggregator, ILogger<MainWindowViewModel> logger, IDialogCoordinator dialogCoordinator,
            IOverlayHandler overlayHandler, IScreenCaptureHandler screenCaptureHandler, IScreenProcessHandler screenProcessHandler, IReleaseManager releaseManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ReleaseInfoUpdatedEvent>().Subscribe(HandleReleaseInfoUpdatedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _dialogCoordinator = dialogCoordinator;
            _overlayHandler = overlayHandler;
            _releaseManager = releaseManager;
            _screenCaptureHandler = screenCaptureHandler;
            _screenProcessHandler = screenProcessHandler;

            // Init View commands
            ApplicationLoadedCommand = new DelegateCommand(ApplicationLoadedExecute);
            LaunchGitHubCommand = new DelegateCommand(LaunchGitHubExecute);
            LaunchKofiCommand = new DelegateCommand(LaunchKofiExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand ApplicationLoadedCommand { get; }
        public DelegateCommand LaunchGitHubCommand { get; }
        public DelegateCommand LaunchKofiCommand { get; }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ApplicationLoadedExecute()
        {
            _logger.LogInformation(WindowTitle);

            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Publish();
        }

        private void HandleReleaseInfoUpdatedEvent()
        {
            var release = _releaseManager.Releases.First();
            if (release != null) 
            {
                string currentVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";
                string latestVersion = release.Version;
                if (!currentVersion.Equals(latestVersion))
                {
                    WindowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version} ({release.Version} available)";
                    _eventAggregator.GetEvent<InfoOccurredEvent>().Publish(new InfoOccurredEventParams
                    {
                        Message = $"New version available: {latestVersion}"
                    });

                    // Open update dialog
                    if (File.Exists("D4Companion.Updater.exe"))
                    {
                        _dialogCoordinator.ShowMessageAsync(this, $"Update", $"New version available, do you want to download {release.Version}?", MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
                        {
                            if (t.Result == MessageDialogResult.Affirmative)
                            {
                                string url = release.Assets.FirstOrDefault(a => a.ContentType.Equals("application/x-zip-compressed"))?.BrowserDownloadUrl ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(url))
                                {
                                    _logger.LogInformation($"Starting D4Companion.Updater.exe. Launch arguments: --url \"{url}\"");
                                    Process.Start("D4Companion.Updater.exe", $"--url \"{url}\"");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"Update process canceled by user.");
                            }
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Cannot update application, D4Companion.Updater.exe not available.");
                    }
                }
            }
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
