using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using NHotkey;
using NHotkey.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace D4Companion.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IBuildsManagerD4Builds _buildsManagerD4Builds;
        private readonly IOverlayHandler _overlayHandler;
        private readonly IReleaseManager _releaseManager;
        private readonly IScreenCaptureHandler _screenCaptureHandler;
        private readonly IScreenProcessHandler _screenProcessHandler;
        private readonly ISettingsManager _settingsManager;

        private string _windowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version}";

        // Start of Constructors region

        #region Constructors

        public MainWindowViewModel(IEventAggregator eventAggregator, ILogger<MainWindowViewModel> logger, IDialogCoordinator dialogCoordinator,
            IOverlayHandler overlayHandler, IScreenCaptureHandler screenCaptureHandler, IScreenProcessHandler screenProcessHandler, 
            ISettingsManager settingsManager, IReleaseManager releaseManager, IBuildsManagerD4Builds buildsManagerD4Builds)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ReleaseInfoUpdatedEvent>().Subscribe(HandleReleaseInfoUpdatedEvent);
            _eventAggregator.GetEvent<TopMostStateChangedEvent>().Subscribe(HandleTopMostStateChangedEvent);
            _eventAggregator.GetEvent<UpdateHotkeysRequestEvent>().Subscribe(HandleUpdateHotkeysRequestEvent);

            // Init logger
            _logger = logger;

            // Init services
            _buildsManagerD4Builds = buildsManagerD4Builds;
            _dialogCoordinator = dialogCoordinator;
            _overlayHandler = overlayHandler;
            _releaseManager = releaseManager;
            _screenCaptureHandler = screenCaptureHandler;
            _screenProcessHandler = screenProcessHandler;
            _settingsManager = settingsManager;

            // Init View commands
            ApplicationLoadedCommand = new DelegateCommand(ApplicationLoadedExecute);
            LaunchGitHubCommand = new DelegateCommand(LaunchGitHubExecute);
            LaunchGitHubWikiCommand = new DelegateCommand(LaunchGitHubWikiExecute);
            LaunchKofiCommand = new DelegateCommand(LaunchKofiExecute);
            WindowClosingCommand = new DelegateCommand(WindowClosingExecute);

            // Init Key bindings
            InitKeyBindings();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand ApplicationLoadedCommand { get; }
        public DelegateCommand LaunchGitHubCommand { get; }
        public DelegateCommand LaunchGitHubWikiCommand { get; }
        public DelegateCommand LaunchKofiCommand { get; }
        public DelegateCommand WindowClosingCommand { get; }

        public bool IsTopMost
        {
            get => _settingsManager.Settings.IsTopMost;
        }
        
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
            var release = _releaseManager?.Releases?.First();
            if (release != null)
            {
                var latest  = Version.Parse(release.Version[1..]);
                var current = Assembly.GetExecutingAssembly().GetName().Version;

                if (latest > current)
                {
                    WindowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version} ({release.Version} available)";
                    _eventAggregator.GetEvent<InfoOccurredEvent>().Publish(new InfoOccurredEventParams
                    {
                        Message = $"New version available: {release.Version}"
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
            else
            {
                _logger.LogWarning("Version information not available.");
            }
        }

        private void HandleTopMostStateChangedEvent()
        {
            RaisePropertyChanged(nameof(IsTopMost));
        }

        private void HandleUpdateHotkeysRequestEvent()
        {
            InitKeyBindings();
        }

        private void HotkeyManager_HotkeyAlreadyRegistered(object? sender, HotkeyAlreadyRegisteredEventArgs hotkeyAlreadyRegisteredEventArgs)
        {
            _logger.LogWarning($"The hotkey {hotkeyAlreadyRegisteredEventArgs.Name} is already registered by another application.");
            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
            {
                Message = $"The hotkey \"{hotkeyAlreadyRegisteredEventArgs.Name}\" is already registered by another application."
            });
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

        private void LaunchGitHubWikiExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/josdemmers/Diablo4Companion/wiki") { UseShellExecute = true });
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

        private void SwitchPresetKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            _eventAggregator.GetEvent<SwitchPresetKeyBindingEvent>().Publish();
        }

        private void ToggleOverlayKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            _eventAggregator.GetEvent<ToggleOverlayKeyBindingEvent>().Publish();
        }

        private void WindowClosingExecute()
        {
            _buildsManagerD4Builds.Dispose();
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitKeyBindings()
        {
            try
            {
                KeyBindingConfig switchPresetKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigSwitchPreset;
                KeyBindingConfig toggleOverlayKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigToggleOverlay;

                HotkeyManager.HotkeyAlreadyRegistered += HotkeyManager_HotkeyAlreadyRegistered;

                KeyGesture switchPresetKeyGesture = new KeyGesture(switchPresetKeyBindingConfig.KeyGestureKey, switchPresetKeyBindingConfig.KeyGestureModifier);
                KeyGesture toggleOverlayKeyGesture = new KeyGesture(toggleOverlayKeyBindingConfig.KeyGestureKey, toggleOverlayKeyBindingConfig.KeyGestureModifier);

                if (switchPresetKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(switchPresetKeyBindingConfig.Name, switchPresetKeyGesture, SwitchPresetKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(switchPresetKeyBindingConfig.Name);
                }

                if (toggleOverlayKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(toggleOverlayKeyBindingConfig.Name, toggleOverlayKeyGesture, ToggleOverlayKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(toggleOverlayKeyBindingConfig.Name);
                }
            }
            catch (HotkeyAlreadyRegisteredException exception) 
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"The hotkey \"{exception.Name}\" is already registered by another application."
                });
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
