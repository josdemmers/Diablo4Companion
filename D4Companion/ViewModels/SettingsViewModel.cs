using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.ViewModels.Dialogs;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System;
using D4Companion.Localization;

namespace D4Companion.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private int? _badgeCount = null;

        private ObservableCollection<AppLanguage> _appLanguages = new ObservableCollection<AppLanguage>();
        private ObservableCollection<SystemPreset> _communitySystemPresets = new ObservableCollection<SystemPreset>();
        private ObservableCollection<string> _systemPresets = new ObservableCollection<string>();

        private bool _downloadInProgress;
        private AppLanguage _selectedAppLanguage = new AppLanguage();
        private SystemPreset _selectedCommunityPreset = new SystemPreset();
        private bool _systemPresetChangeAllowed = true;

        // Start of Constructors region

        #region Constructors

        public SettingsViewModel(IEventAggregator eventAggregator, ILogger<SettingsViewModel> logger, IDialogCoordinator dialogCoordinator, 
            ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<DownloadSystemPresetCompletedEvent>().Subscribe(HandleDownloadSystemPresetCompletedEvent);
            _eventAggregator.GetEvent<SystemPresetExtractedEvent>().Subscribe(HandleSystemPresetExtractedEvent);
            _eventAggregator.GetEvent<SystemPresetInfoUpdatedEvent>().Subscribe(HandleSystemPresetInfoUpdatedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);

            // Init logger
            _logger = logger;

            // Init services
            _dialogCoordinator = dialogCoordinator;
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init view commands
            DownloadSystemPresetCommand = new DelegateCommand(DownloadSystemPresetExecute, CanDownloadSystemPresetExecute);
            ReloadSystemPresetImagesCommand = new DelegateCommand(ReloadSystemPresetImagesExecute, CanReloadSystemPresetImagesExecute);
            SetControllerConfigCommand = new DelegateCommand(SetControllerConfigExecute);
            SetHotkeysCommand = new DelegateCommand(SetHotkeysExecute);
            SetOverlayConfigCommand = new DelegateCommand(SetOverlayConfigExecute);

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

        public DelegateCommand DownloadSystemPresetCommand { get; }
        public DelegateCommand ReloadSystemPresetImagesCommand { get; }
        public DelegateCommand SetControllerConfigCommand { get; }
        public DelegateCommand SetHotkeysCommand { get; }
        public DelegateCommand SetOverlayConfigCommand { get; }

        public ObservableCollection<AppLanguage> AppLanguages { get => _appLanguages; set => _appLanguages = value; }
        public ObservableCollection<SystemPreset> CommunitySystemPresets { get => _communitySystemPresets; set => _communitySystemPresets = value; }
        public ObservableCollection<string> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public bool IsAspectCounterEnabled
        {
            get => _settingsManager.Settings.AspectCounter;
            set
            {
                _settingsManager.Settings.AspectCounter = value;
                RaisePropertyChanged(nameof(IsAspectCounterEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsCheckForUpdatesEnabled
        {
            get => _settingsManager.Settings.CheckForUpdates;
            set
            {
                _settingsManager.Settings.CheckForUpdates = value;
                RaisePropertyChanged(nameof(IsCheckForUpdatesEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsControllerModeEnabled
        {
            get => _settingsManager.Settings.ControllerMode;
            set
            {
                _settingsManager.Settings.ControllerMode = value;
                RaisePropertyChanged(nameof(IsControllerModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsDebugModeEnabled
        {
            get => _settingsManager.Settings.DebugMode;
            set
            {
                _settingsManager.Settings.DebugMode = value;
                RaisePropertyChanged(nameof(IsDebugModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsDevModeEnabled
        {
            get => _settingsManager.Settings.DevMode;
            set
            {
                _settingsManager.Settings.DevMode = value;
                RaisePropertyChanged(nameof(IsDevModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsExperimentalConsumablesModeEnabled
        {
            get => _settingsManager.Settings.ExperimentalModeConsumables;
            set
            {
                _settingsManager.Settings.ExperimentalModeConsumables = value;
                RaisePropertyChanged(nameof(IsExperimentalConsumablesModeEnabled));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<ExperimentalConsumablesChangedEvent>().Publish();
            }
        }

        public bool IsExperimentalSeasonalModeEnabled
        {
            get => _settingsManager.Settings.ExperimentalModeSeasonal;
            set
            {
                _settingsManager.Settings.ExperimentalModeSeasonal = value;
                RaisePropertyChanged(nameof(IsExperimentalSeasonalModeEnabled));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<ExperimentalSeasonalChangedEvent>().Publish();
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
                RaisePropertyChanged(nameof(SelectedAppLanguage));
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
                    _settingsManager.Settings.SelectedSystemPreset = value;
                    RaisePropertyChanged(nameof(SelectedSystemPreset));

                    _settingsManager.SaveSettings();

                    _eventAggregator.GetEvent<SystemPresetChangedEvent>().Publish();

                    DownloadSystemPresetCommand?.RaiseCanExecuteChanged();
                    RaisePropertyChanged(nameof(PresetDownloadButtonCaption));
                }
            }
        }

        public bool SystemPresetChangeAllowed
        {
            get => _systemPresetChangeAllowed;
            set
            {
                _systemPresetChangeAllowed = value;
                RaisePropertyChanged(nameof(SystemPresetChangeAllowed));
                DownloadSystemPresetCommand?.RaiseCanExecuteChanged();
                ReloadSystemPresetImagesCommand?.RaiseCanExecuteChanged();
            }
        }

        public SystemPreset SelectedCommunityPreset
        {
            get => _selectedCommunityPreset;
            set
            {
                _selectedCommunityPreset = value;
                RaisePropertyChanged(nameof(SelectedCommunityPreset));
                DownloadSystemPresetCommand?.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(PresetDownloadButtonCaption));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleDownloadSystemPresetCompletedEvent(string fileName)
        {
            Task.Factory.StartNew(() =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _systemPresetManager.ExtractSystemPreset(fileName);
                });
            });
        }

        private void HandleSystemPresetExtractedEvent()
        {
            _downloadInProgress = false;
            DownloadSystemPresetCommand?.RaiseCanExecuteChanged();

            InitSystemPresets();

            // Reload image data for current system preset.
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Publish();
        }

        private void HandleSystemPresetInfoUpdatedEvent()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                CommunitySystemPresets.Clear();
                CommunitySystemPresets.AddRange(_systemPresetManager.SystemPresets);
            });
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SystemPresetChangeAllowed = !toggleOverlayEventParams.IsEnabled;
            });
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SystemPresetChangeAllowed = !toggleOverlayFromGUIEventParams.IsEnabled;
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
            //_appLanguages.Add(new AppLanguage("es-ES", "Spanish (EU)"));
            //_appLanguages.Add(new AppLanguage("es-MX", "Spanish (LA)"));
            //_appLanguages.Add(new AppLanguage("fr-FR", "French"));
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

                // Restore previvous selection.
                SelectedSystemPreset = SystemPresets.FirstOrDefault(preset => preset.Equals(previousSelectedSystemPreset)) ?? previousSelectedSystemPreset;
            });   
        }

        private bool CanDownloadSystemPresetExecute()
        {
            return SystemPresetChangeAllowed && !_downloadInProgress && SelectedCommunityPreset != null && !string.IsNullOrWhiteSpace(SelectedCommunityPreset.FileName);
        }

        private void DownloadSystemPresetExecute()
        {
            _downloadInProgress = true;
            DownloadSystemPresetCommand?.RaiseCanExecuteChanged();

            Task.Factory.StartNew(() =>
            {
                _systemPresetManager.DownloadSystemPreset(SelectedCommunityPreset.FileName);
            });
        }

        private bool CanReloadSystemPresetImagesExecute()
        {
            return SystemPresetChangeAllowed;
        }

        private void ReloadSystemPresetImagesExecute()
        {
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Publish();
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
            RaisePropertyChanged(nameof(IsAspectCounterEnabled));
        }

        #endregion
    }
}
