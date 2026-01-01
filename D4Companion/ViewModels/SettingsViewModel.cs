using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.Messages;
using D4Companion.ViewModels.Dialogs;
using D4Companion.Views.Dialogs;
using Emgu.CV;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private int? _badgeCount = null;
        private bool _isPresetUpdateReady = false;

        private ObservableCollection<AppLanguage> _appLanguages = new ObservableCollection<AppLanguage>();
        private ObservableCollection<SystemPreset> _communitySystemPresets = new ObservableCollection<SystemPreset>();
        private ObservableCollection<string> _systemPresets = new ObservableCollection<string>();

        private bool _downloadInProgress;
        private bool _loadDefaultConfig = true;
        private AppLanguage _selectedAppLanguage = new AppLanguage();
        private SystemPreset _selectedCommunityPreset = new SystemPreset();
        private bool _systemPresetChangeAllowed = true;

        // Start of Constructors region

        #region Constructors

        public SettingsViewModel(ILogger<SettingsViewModel> logger, IDialogCoordinator dialogCoordinator, 
            ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init services
            _dialogCoordinator = dialogCoordinator;
            _logger = logger;
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<DownloadSystemPresetCompletedMessage>(this, HandleDownloadSystemPresetCompletedMessage);
            WeakReferenceMessenger.Default.Register<SwitchOverlayKeyBindingMessage>(this, HandleSwitchOverlayKeyBindingMessage);
            WeakReferenceMessenger.Default.Register<SystemPresetExtractedMessage>(this, HandleSystemPresetExtractedMessage);
            WeakReferenceMessenger.Default.Register<SystemPresetInfoUpdatedMessage>(this, HandleSystemPresetInfoUpdatedMessage);
            WeakReferenceMessenger.Default.Register<ToggleControllerKeyBindingMessage>(this, HandleToggleControllerKeyBindingMessage);
            WeakReferenceMessenger.Default.Register<ToggleOverlayMessage>(this, HandleToggleOverlayMessage);
            WeakReferenceMessenger.Default.Register<ToggleOverlayFromGUIMessage>(this, HandleToggleOverlayFromGUIMessage);

            // Init view commands
            DownloadSystemPresetCommand = new RelayCommand(DownloadSystemPresetExecute, CanDownloadSystemPresetExecute);
            SetControllerConfigCommand = new RelayCommand(SetControllerConfigExecute);
            SetColorsCommand = new RelayCommand(SetColorsExecute);
            SetHotkeysCommand = new RelayCommand(SetHotkeysExecute);
            SetOverlayConfigCommand = new RelayCommand(SetOverlayConfigExecute);
            SetParagonConfigCommand = new RelayCommand(SetParagonConfigExecute);

            // Init presets
            InitSystemPresets();

            // Init affix languages
            InitApplanguages();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand DownloadSystemPresetCommand { get; }
        public ICommand SetControllerConfigCommand { get; }
        public ICommand SetColorsCommand { get; }
        public ICommand SetHotkeysCommand { get; }
        public ICommand SetOverlayConfigCommand { get; }
        public ICommand SetParagonConfigCommand { get; }

        public ObservableCollection<AppLanguage> AppLanguages { get => _appLanguages; set => _appLanguages = value; }
        public ObservableCollection<SystemPreset> CommunitySystemPresets { get => _communitySystemPresets; set => _communitySystemPresets = value; }
        public ObservableCollection<string> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public bool IsCheckForUpdatesEnabled
        {
            get => _settingsManager.Settings.CheckForUpdates;
            set
            {
                _settingsManager.Settings.CheckForUpdates = value;
                OnPropertyChanged(nameof(IsCheckForUpdatesEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsControllerModeEnabled
        {
            get => _settingsManager.Settings.ControllerMode;
            set
            {
                _settingsManager.Settings.ControllerMode = value;
                OnPropertyChanged(nameof(IsControllerModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsDebugModeEnabled
        {
            get => _settingsManager.Settings.DebugMode;
            set
            {
                _settingsManager.Settings.DebugMode = value;
                OnPropertyChanged(nameof(IsDebugModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsDevModeEnabled
        {
            get => _settingsManager.Settings.DevMode;
            set
            {
                _settingsManager.Settings.DevMode = value;
                OnPropertyChanged(nameof(IsDevModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsLaunchMinimizedEnabled
        {
            get => _settingsManager.Settings.LaunchMinimized;
            set
            {
                _settingsManager.Settings.LaunchMinimized = value;
                OnPropertyChanged(nameof(IsLaunchMinimizedEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsMinimizeToTrayEnabled
        {
            get => _settingsManager.Settings.MinimizeToTray;
            set
            {
                _settingsManager.Settings.MinimizeToTray = value;
                OnPropertyChanged(nameof(IsMinimizeToTrayEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsParagonModeActive
        {
            get => _settingsManager.Settings.IsParagonModeActive;
            set
            {
                _settingsManager.Settings.IsParagonModeActive = value;
                OnPropertyChanged(nameof(IsParagonModeActive));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsPresetUpdateReady
        {
            get => _isPresetUpdateReady;
            set
            {
                _isPresetUpdateReady = value;
                OnPropertyChanged(nameof(IsPresetUpdateReady));
            }
        }

        public string PresetDownloadButtonCaption
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SelectedCommunityPreset.FileName) && SelectedSystemPreset.Equals(Path.GetFileNameWithoutExtension(SelectedCommunityPreset.FileName)) ? "Update" : "Download";
            }
        }

        public AppLanguage SelectedAppLanguage
        {
            get => _selectedAppLanguage;
            set
            {
                _selectedAppLanguage = value;
                OnPropertyChanged(nameof(SelectedAppLanguage));
                if (value != null)
                {
                    _settingsManager.Settings.SelectedAppLanguage = value.Id;
                    _settingsManager.SaveSettings();

                    TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo(SelectedAppLanguage.Id);
                }
            }
        }

        public string SelectedSystemPreset
        {
            get => _settingsManager.Settings.SelectedSystemPreset;
            set
            {
                if (value != null)
                {
                    _logger.LogInformation($"Current system preset: {value}");

                    _settingsManager.Settings.SelectedSystemPreset = value;
                    OnPropertyChanged(nameof(SelectedSystemPreset));

                    if (_loadDefaultConfig)
                    {
                        string fileName = $"Images/{SelectedSystemPreset}/config.json";
                        if (File.Exists(fileName))
                        {
                            using FileStream stream = File.OpenRead(fileName);
                            var systemPresetDefaults = JsonSerializer.Deserialize<SystemPresetDefaults>(stream) ?? new SystemPresetDefaults();
                            _settingsManager.Settings.AffixAreaHeightOffsetTop = systemPresetDefaults.AffixAreaHeightOffsetTop;
                            _settingsManager.Settings.AffixAreaHeightOffsetBottom = systemPresetDefaults.AffixAreaHeightOffsetBottom;
                            _settingsManager.Settings.AffixAspectAreaWidthOffset = systemPresetDefaults.AffixAspectAreaWidthOffset;
                            _settingsManager.Settings.AspectAreaHeightOffsetTop = systemPresetDefaults.AspectAreaHeightOffsetTop;
                            _settingsManager.Settings.ParagonLeftOffsetCollapsed = systemPresetDefaults.ParagonLeftOffsetCollapsed;
                            _settingsManager.Settings.ParagonNodeSize = systemPresetDefaults.ParagonNodeSize;
                            _settingsManager.Settings.ParagonNodeSizeCollapsed = systemPresetDefaults.ParagonNodeSizeCollapsed;
                            _settingsManager.Settings.ParagonTopOffsetCollapsed = systemPresetDefaults.ParagonTopOffsetCollapsed;
                            _settingsManager.Settings.ThresholdMin = systemPresetDefaults.ThresholdMin;
                            _settingsManager.Settings.ThresholdMax = systemPresetDefaults.ThresholdMax;
                            _settingsManager.Settings.TooltipWidth = systemPresetDefaults.TooltipWidth;
                            _settingsManager.Settings.TooltipMaxHeight = systemPresetDefaults.TooltipHeightType;
                        }
                    }

                    _settingsManager.SaveSettings();

                    WeakReferenceMessenger.Default.Send(new SystemPresetChangedMessage());

                    ((RelayCommand)DownloadSystemPresetCommand).NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(PresetDownloadButtonCaption));
                }
            }
        }

        public bool SystemPresetChangeAllowed
        {
            get => _systemPresetChangeAllowed;
            set
            {
                _systemPresetChangeAllowed = value;
                OnPropertyChanged(nameof(SystemPresetChangeAllowed));
                ((RelayCommand)DownloadSystemPresetCommand).NotifyCanExecuteChanged();
            }
        }

        public SystemPreset SelectedCommunityPreset
        {
            get => _selectedCommunityPreset;
            set
            {
                _selectedCommunityPreset = value;
                OnPropertyChanged(nameof(SelectedCommunityPreset));
                ((RelayCommand)DownloadSystemPresetCommand).NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(PresetDownloadButtonCaption));
                IsPresetUpdateReady = false;
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleDownloadSystemPresetCompletedMessage(object recipient, DownloadSystemPresetCompletedMessage message)
        {
            string fileName = message.Value;

            Task.Factory.StartNew(() =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _systemPresetManager.ExtractSystemPreset(fileName);
                });
            });
        }

        private void HandleSwitchOverlayKeyBindingMessage(object recipient, SwitchOverlayKeyBindingMessage message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsParagonModeActive = !IsParagonModeActive;
            });
        }

        private void HandleSystemPresetExtractedMessage(object recipient, SystemPresetExtractedMessage message)
        {
            _downloadInProgress = false;
            ((RelayCommand)DownloadSystemPresetCommand).NotifyCanExecuteChanged();

            InitSystemPresets();

            // Reload image data for current system preset.
            WeakReferenceMessenger.Default.Send(new SystemPresetChangedMessage());

            // Notify user
            IsPresetUpdateReady = true;
        }

        private void HandleSystemPresetInfoUpdatedMessage(object recipient, SystemPresetInfoUpdatedMessage message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                CommunitySystemPresets.Clear();
                CommunitySystemPresets.AddRange(_systemPresetManager.SystemPresets);
            });
        }

        private void HandleToggleControllerKeyBindingMessage(object recipient, ToggleControllerKeyBindingMessage message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsControllerModeEnabled = !IsControllerModeEnabled;
            });
        }

        private void HandleToggleOverlayMessage(object recipient, ToggleOverlayMessage message)
        {
            var toggleOverlayMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SystemPresetChangeAllowed = !toggleOverlayMessageParams.IsEnabled;
            });
        }

        private void HandleToggleOverlayFromGUIMessage(object recipient, ToggleOverlayFromGUIMessage message)
        {
            var toggleOverlayFromGUIMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SystemPresetChangeAllowed = !toggleOverlayFromGUIMessageParams.IsEnabled;
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitApplanguages()
        {
            _appLanguages.Clear();
            _appLanguages.Add(new AppLanguage("de-DE", "German"));
            _appLanguages.Add(new AppLanguage("en-US", "English"));
            _appLanguages.Add(new AppLanguage("es-ES", "Spanish (EU)"));
            //_appLanguages.Add(new AppLanguage("es-MX", "Spanish (LA)"));
            _appLanguages.Add(new AppLanguage("fr-FR", "French"));
            //_appLanguages.Add(new AppLanguage("it-IT", "Italian"));
            //_appLanguages.Add(new AppLanguage("ja-JP", "Japanese"));
            //_appLanguages.Add(new AppLanguage("ko-KR", "Korean"));
            //_appLanguages.Add(new AppLanguage("pl-PL", "Polish"));
            //_appLanguages.Add(new AppLanguage("pt-BR", "Portuguese"));
            //_appLanguages.Add(new AppLanguage("ru-RU", "Russian"));
            //_appLanguages.Add(new AppLanguage("tr-TR", "Turkish"));
            _appLanguages.Add(new AppLanguage("zh-CN", "Chinese (Simplified)"));
            //_appLanguages.Add(new AppLanguage("zh-TW", "Chinese (Traditional)"));

            var language = _appLanguages.FirstOrDefault(language => language.Id.Equals(_settingsManager.Settings.SelectedAppLanguage));
            if (language != null)
            {
                SelectedAppLanguage = language;
            }
        }

        private void InitSystemPresets()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousSelectedSystemPreset = SelectedSystemPreset;
                _systemPresets.Clear();

                string directory = $"Images\\";
                if (Directory.Exists(directory))
                {
                    string[] directoryEntries = Directory.GetDirectories(directory, "*p_*").Select(d => new DirectoryInfo(d).Name).ToArray();
                    foreach (string directoryName in directoryEntries)
                    {
                        if (!string.IsNullOrWhiteSpace(directoryName))
                        {
                            _systemPresets.Add(directoryName);
                        }
                    }
                }

                // Restore previous selection.
                _loadDefaultConfig = false;
                SelectedSystemPreset = SystemPresets.FirstOrDefault(preset => preset.Equals(previousSelectedSystemPreset)) ?? previousSelectedSystemPreset;
                _loadDefaultConfig = true;
            });   
        }

        private bool CanDownloadSystemPresetExecute()
        {
            return SystemPresetChangeAllowed && !_downloadInProgress && SelectedCommunityPreset != null && !string.IsNullOrWhiteSpace(SelectedCommunityPreset.FileName);
        }

        private void DownloadSystemPresetExecute()
        {
            IsPresetUpdateReady = false;
            _downloadInProgress = true;
            ((RelayCommand)DownloadSystemPresetCommand).NotifyCanExecuteChanged();

            Task.Factory.StartNew(() =>
            {
                _systemPresetManager.DownloadSystemPreset(SelectedCommunityPreset.FileName);
            });
        }

        private async void SetControllerConfigExecute()
        {
            var controllerConfigDialog = new CustomDialog() { Title = "Controller config" };
            var dataContext = new ControllerConfigViewModel(async instance =>
            {
                await controllerConfigDialog.WaitUntilUnloadedAsync();
            });
            controllerConfigDialog.Content = new ControllerConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, controllerConfigDialog);
            await controllerConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        private async void SetColorsExecute()
        {
            var colorsConfigDialog = new CustomDialog() { Title = "Default colors config" };
            var dataContext = new ColorsConfigViewModel(async instance =>
            {
                await colorsConfigDialog.WaitUntilUnloadedAsync();
            });
            colorsConfigDialog.Content = new ColorsConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, colorsConfigDialog);
            await colorsConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        private async void SetHotkeysExecute()
        {
            var hotkeysConfigDialog = new CustomDialog() { Title = "Hotkeys config" };
            var dataContext = new HotkeysConfigViewModel(async instance =>
            {
                await hotkeysConfigDialog.WaitUntilUnloadedAsync();
            });
            hotkeysConfigDialog.Content = new HotkeysConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, hotkeysConfigDialog);
            await hotkeysConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        private async void SetOverlayConfigExecute()
        {
            var overlayConfigDialog = new CustomDialog() { Title = "Overlay config" };
            var dataContext = new OverlayConfigViewModel(async instance =>
            {
                await overlayConfigDialog.WaitUntilUnloadedAsync();
            });
            overlayConfigDialog.Content = new OverlayConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, overlayConfigDialog);
            await overlayConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        private async void SetParagonConfigExecute()
        {
            var paragonConfigDialog = new CustomDialog() { Title = "Paragon config" };
            var dataContext = new ParagonConfigViewModel(async instance =>
            {
                await paragonConfigDialog.WaitUntilUnloadedAsync();
            });
            paragonConfigDialog.Content = new ParagonConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, paragonConfigDialog);
            await paragonConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        #endregion
    }
}
