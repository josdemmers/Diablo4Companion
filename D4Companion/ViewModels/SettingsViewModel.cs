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

namespace D4Companion.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _systemPresets = new ObservableCollection<string>();

        private bool _systemPresetChangeAllowed = true;

        // Start of Constructors region

        #region Constructors

        public SettingsViewModel(IEventAggregator eventAggregator, ILogger<SettingsViewModel> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Init presets
            InitSystemPresets();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<string> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

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

        public string SelectedSystemPreset
        {
            get => _settingsManager.Settings.SelectedSystemPreset;
            set
            {
                _settingsManager.Settings.SelectedSystemPreset = value;
                RaisePropertyChanged(nameof(SelectedSystemPreset));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<SystemPresetChangedEvent>().Publish();
                _eventAggregator.GetEvent<ReloadAffixesGuiRequestEvent>().Publish();
            }
        }

        public bool SystemPresetChangeAllowed
        {
            get => _systemPresetChangeAllowed;
            set
            {
                _systemPresetChangeAllowed = value;
                RaisePropertyChanged(nameof(SystemPresetChangeAllowed));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            SystemPresetChangeAllowed = !toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            SystemPresetChangeAllowed = !toggleOverlayFromGUIEventParams.IsEnabled;
        }      

        #endregion

        // Start of Methods region

        #region Methods

        private void InitSystemPresets()
        {
            // Item affixes
            string directory = $"Images\\";
            if (Directory.Exists(directory))
            {
                string[] directoryEntries = Directory.GetDirectories(directory,"*x*").Select(d => new DirectoryInfo(d).Name).ToArray();
                foreach (string directoryName in directoryEntries)
                {
                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        _systemPresets.Add(directoryName);
                    }                    
                }
            }
        }

        #endregion
    }
}
