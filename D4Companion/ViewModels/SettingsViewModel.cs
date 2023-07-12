using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Services;
using Emgu.CV.Structure;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using D4Companion.Events;
using System.Windows;
using Prism.Commands;
using System.Threading.Tasks;

namespace D4Companion.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private int? _badgeCount = null;

        private ObservableCollection<string> _systemPresets = new ObservableCollection<string>();
        private ObservableCollection<SystemPreset> _communitySystemPresets = new ObservableCollection<SystemPreset>();

        private bool _downloadInProgress;
        private SystemPreset _selectedCommunityPreset = new SystemPreset();
        private bool _systemPresetChangeAllowed = true;

        // Start of Constructors region

        #region Constructors

        public SettingsViewModel(IEventAggregator eventAggregator, ILogger<SettingsViewModel> logger, ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
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
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init view commands
            DownloadSystemPresetCommand = new DelegateCommand(DownloadSystemPresetExecute, CanDownloadSystemPresetExecute);
            ReloadSystemPresetImagesCommand = new DelegateCommand(ReloadSystemPresetImagesExecute, CanReloadSystemPresetImagesExecute);

            // Init presets
            InitSystemPresets();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand DownloadSystemPresetCommand { get; }
        public DelegateCommand ReloadSystemPresetImagesCommand { get; }

        public ObservableCollection<string> SystemPresets { get => _systemPresets; set => _systemPresets = value; }
        public ObservableCollection<SystemPreset> CommunitySystemPresets { get => _communitySystemPresets; set => _communitySystemPresets = value; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

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

        public bool IsLiteModeEnabled
        {
            get => _settingsManager.Settings.LiteMode;
            set
            {
                _settingsManager.Settings.LiteMode = value;
                RaisePropertyChanged(nameof(IsLiteModeEnabled));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<ReloadAffixesGuiRequestEvent>().Publish();
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
                    _eventAggregator.GetEvent<ReloadAffixesGuiRequestEvent>().Publish();

                    DownloadSystemPresetCommand?.RaiseCanExecuteChanged();
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
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleDownloadSystemPresetCompletedEvent(string fileName)
        {
            Task.Factory.StartNew(() =>
            {
                _systemPresetManager.ExtractSystemPreset(fileName);
            });
        }

        private void HandleSystemPresetExtractedEvent()
        {
            _downloadInProgress = false;
            DownloadSystemPresetCommand?.RaiseCanExecuteChanged();

            InitSystemPresets();
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

        private void InitSystemPresets()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousSelectedSystemPreset = SelectedSystemPreset;
                _systemPresets.Clear();

                string directory = $"Images\\";
                if (Directory.Exists(directory))
                {
                    string[] directoryEntries = Directory.GetDirectories(directory, "*x*").Select(d => new DirectoryInfo(d).Name).ToArray();
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
            return SystemPresetChangeAllowed && !_downloadInProgress && SelectedCommunityPreset != null && 
                !string.IsNullOrWhiteSpace(SelectedCommunityPreset.FileName) && !SelectedSystemPreset.Equals(Path.GetFileNameWithoutExtension(SelectedCommunityPreset.FileName));
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
            _eventAggregator.GetEvent<ReloadAffixesGuiRequestEvent>().Publish();
        }

        #endregion
    }
}
