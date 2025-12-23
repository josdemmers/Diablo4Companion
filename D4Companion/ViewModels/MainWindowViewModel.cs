using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Messages;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
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

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IDialogCoordinator dialogCoordinator,
            IOverlayHandler overlayHandler, IScreenCaptureHandler screenCaptureHandler, IScreenProcessHandler screenProcessHandler, 
            ISettingsManager settingsManager, IReleaseManager releaseManager, IBuildsManagerD4Builds buildsManagerD4Builds)
        {
            // Init services
            _buildsManagerD4Builds = buildsManagerD4Builds;
            _dialogCoordinator = dialogCoordinator;
            _logger = logger;
            _overlayHandler = overlayHandler;
            _releaseManager = releaseManager;
            _screenCaptureHandler = screenCaptureHandler;
            _screenProcessHandler = screenProcessHandler;
            _settingsManager = settingsManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<ReleaseInfoUpdatedMessage>(this, HandleReleaseInfoUpdatedMessage);
            WeakReferenceMessenger.Default.Register<TopMostStateChangedMessage>(this, HandleTopMostStateChangedMessage);
            WeakReferenceMessenger.Default.Register<UpdateHotkeysRequestMessage>(this, HandleUpdateHotkeysRequestMessage);

            // Init view commands
            ApplicationLoadedCommand = new RelayCommand(ApplicationLoadedExecute);
            LaunchGitHubCommand = new RelayCommand(LaunchGitHubExecute);
            LaunchGitHubWikiCommand = new RelayCommand(LaunchGitHubWikiExecute);
            LaunchKofiCommand = new RelayCommand(LaunchKofiExecute);
            NotifyIconDoubleClickCommand = new RelayCommand(NotifyIconDoubleClickExecute);
            NotifyIconOpenCommand = new RelayCommand(NotifyIconOpenExecute);
            NotifyIconExitCommand = new RelayCommand(NotifyIconExitExecute);
            WindowClosingCommand = new RelayCommand(WindowClosingExecute);
            WindowStateChangedCommand = new RelayCommand(WindowStateChangedExecute);

            // Init Key bindings
            InitKeyBindings();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand ApplicationLoadedCommand { get; }
        public ICommand LaunchGitHubCommand { get; }
        public ICommand LaunchGitHubWikiCommand { get; }
        public ICommand LaunchKofiCommand { get; }
        public ICommand NotifyIconDoubleClickCommand { get; }
        public ICommand NotifyIconOpenCommand { get; }
        public ICommand NotifyIconExitCommand { get; }
        public ICommand WindowClosingCommand { get; }
        public ICommand WindowStateChangedCommand { get; }

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
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ApplicationLoadedExecute()
        {
            _logger.LogInformation(WindowTitle);

            if (_settingsManager.Settings.LaunchMinimized && !_releaseManager.UpdateAvailable)
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            }

            WeakReferenceMessenger.Default.Send(new ApplicationLoadedMessage());
        }

        private void HandleReleaseInfoUpdatedMessage(object recipient, ReleaseInfoUpdatedMessage message)
        {
            var current = Assembly.GetExecutingAssembly().GetName().Version;
            var releases = new List<Release>();
            releases.AddRange(_releaseManager.Releases);
            // Remove all older releases.
            releases.RemoveAll(r => Version.Parse(r.Version[1..]) < current);

            var release = releases.FirstOrDefault();
            if (release != null)
            {
                var latest = Version.Parse(release.Version[1..]);

                if (latest > current)
                {
                    _releaseManager.UpdateAvailable = true;
                    WindowTitle = $"Diablo IV Companion v{Assembly.GetExecutingAssembly().GetName().Version} ({release.Version} available)";
                    WeakReferenceMessenger.Default.Send(new InfoOccurredMessage(new InfoOccurredMessageParams
                    {
                        Message = $"New version available: {release.Version}"
                    }));

                    // Open update dialog
                    if (File.Exists("D4Companion.Updater.exe"))
                    {
                        // Restore GUI when hidden.
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            if (Application.Current.MainWindow.Visibility == Visibility.Collapsed)
                            {
                                Application.Current.MainWindow.Show();
                                Application.Current.MainWindow.WindowState = WindowState.Normal;
                                Application.Current.MainWindow.Topmost = true;
                            }
                        });

                        _dialogCoordinator.ShowMessageAsync(this, $"Update", $"New version available, do you want to download {release.Version}?", MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
                        {
                            if (t.Result == MessageDialogResult.Affirmative)
                            {
                                string url = release.Assets.FirstOrDefault(a => a.ContentType.Equals("application/x-zip-compressed"))?.BrowserDownloadUrl ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(url))
                                {
                                    _logger.LogInformation($"Starting D4Companion.Updater.exe. Launch arguments: --url \"{url}\"");
                                    Process.Start("D4Companion.Updater.exe", $"--url \"{url}\"");

                                    Application.Current?.Dispatcher?.Invoke(() =>
                                    {
                                        _logger.LogInformation("Closing D4Companion.exe");
                                        Application.Current.Shutdown();
                                    });
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
                _logger.LogInformation("No new version available.");
            }
        }

        private void HandleTopMostStateChangedMessage(object recipient, TopMostStateChangedMessage message)
        {
            OnPropertyChanged(nameof(IsTopMost));
        }

        private void HandleUpdateHotkeysRequestMessage(object recipient, UpdateHotkeysRequestMessage message)
        {
            InitKeyBindings();
        }

        private void HotkeyManager_HotkeyAlreadyRegistered(object? sender, HotkeyAlreadyRegisteredEventArgs hotkeyAlreadyRegisteredEventArgs)
        {
            _logger.LogWarning($"The hotkey {hotkeyAlreadyRegisteredEventArgs.Name} is already registered by another application.");
            WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
            {
                Message = $"The hotkey \"{hotkeyAlreadyRegisteredEventArgs.Name}\" is already registered by another application."
            }));
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

        private void NotifyIconDoubleClickExecute()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Topmost = true;
            });
        }

        private void NotifyIconOpenExecute()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Topmost = true;
            });
        }

        private void NotifyIconExitExecute()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        private void SwitchPresetKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new SwitchPresetKeyBindingMessage());
        }

        private void SwitchOverlayKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new SwitchOverlayKeyBindingMessage());
        }

        private void TakeScreenshotKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new TakeScreenshotRequestedMessage());
        }

        private void ToggleControllerKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new ToggleControllerKeyBindingMessage());
        }


        private void ToggleOverlayKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new ToggleOverlayKeyBindingMessage());
        }

        private void ToggleDebugLockScreencaptureKeyBindingExecute(object? sender, HotkeyEventArgs hotkeyEventArgs)
        {
            hotkeyEventArgs.Handled = true;
            WeakReferenceMessenger.Default.Send(new ToggleDebugLockScreencaptureKeyBindingMessage());
        }

        private void WindowClosingExecute()
        {
        }

        private void WindowStateChangedExecute()
        {
            if (!_settingsManager.Settings.MinimizeToTray) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                switch(Application.Current.MainWindow.WindowState)
                {
                    case WindowState.Minimized:
                        {
                            if (_releaseManager.UpdateAvailable)
                            {
                                Application.Current.MainWindow.WindowState = WindowState.Normal;
                            }
                            else
                            {
                                Application.Current.MainWindow.Visibility = Visibility.Collapsed;
                            }
                            break;
                        }
                }
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitKeyBindings()
        {
            try
            {
                KeyBindingConfig switchPresetKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigSwitchPreset;
                KeyBindingConfig switchOverlayKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigSwitchOverlay;
                KeyBindingConfig takeScreenshotBindingConfig = _settingsManager.Settings.KeyBindingConfigTakeScreenshot;
                KeyBindingConfig toggleControllerKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigToggleController;
                KeyBindingConfig toggleOverlayKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigToggleOverlay;
                KeyBindingConfig toggleDebugLockScreencaptureKeyBindingConfig = _settingsManager.Settings.KeyBindingConfigToggleDebugLockScreencapture;

                HotkeyManager.HotkeyAlreadyRegistered += HotkeyManager_HotkeyAlreadyRegistered;

                KeyGesture switchPresetKeyGesture = new KeyGesture(switchPresetKeyBindingConfig.KeyGestureKey, switchPresetKeyBindingConfig.KeyGestureModifier);
                KeyGesture switchOverlayKeyGesture = new KeyGesture(switchOverlayKeyBindingConfig.KeyGestureKey, switchOverlayKeyBindingConfig.KeyGestureModifier);
                KeyGesture takeScreenshotKeyGesture = new KeyGesture(takeScreenshotBindingConfig.KeyGestureKey, takeScreenshotBindingConfig.KeyGestureModifier);
                KeyGesture toggleControllerKeyGesture = new KeyGesture(toggleControllerKeyBindingConfig.KeyGestureKey, toggleControllerKeyBindingConfig.KeyGestureModifier);
                KeyGesture toggleOverlayKeyGesture = new KeyGesture(toggleOverlayKeyBindingConfig.KeyGestureKey, toggleOverlayKeyBindingConfig.KeyGestureModifier);
                KeyGesture toggleDebugLockScreencaptureKeyGesture = new KeyGesture(toggleDebugLockScreencaptureKeyBindingConfig.KeyGestureKey, toggleDebugLockScreencaptureKeyBindingConfig.KeyGestureModifier);

                if (switchPresetKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(switchPresetKeyBindingConfig.Name, switchPresetKeyGesture, SwitchPresetKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(switchPresetKeyBindingConfig.Name);
                }

                if (switchOverlayKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(switchOverlayKeyBindingConfig.Name, switchOverlayKeyGesture, SwitchOverlayKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(switchOverlayKeyBindingConfig.Name);
                }

                if (takeScreenshotBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(takeScreenshotBindingConfig.Name, takeScreenshotKeyGesture, TakeScreenshotKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(takeScreenshotBindingConfig.Name);
                }

                if (toggleControllerKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(toggleControllerKeyBindingConfig.Name, toggleControllerKeyGesture, ToggleControllerKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(toggleControllerKeyBindingConfig.Name);
                }

                if (toggleOverlayKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(toggleOverlayKeyBindingConfig.Name, toggleOverlayKeyGesture, ToggleOverlayKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(toggleOverlayKeyBindingConfig.Name);
                }

                if (toggleDebugLockScreencaptureKeyBindingConfig.IsEnabled)
                {
                    HotkeyManager.Current.AddOrReplace(toggleDebugLockScreencaptureKeyBindingConfig.Name, toggleDebugLockScreencaptureKeyGesture, ToggleDebugLockScreencaptureKeyBindingExecute);
                }
                else
                {
                    HotkeyManager.Current.Remove(toggleDebugLockScreencaptureKeyBindingConfig.Name);
                }
            }
            catch (HotkeyAlreadyRegisteredException exception) 
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"The hotkey \"{exception.Name}\" is already registered by another application."
                }));
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
