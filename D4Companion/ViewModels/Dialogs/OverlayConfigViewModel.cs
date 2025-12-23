using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Interfaces;
using D4Companion.Messages;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class OverlayConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _overlayMarkerModes = new ObservableCollection<string>();
        private ObservableCollection<string> _sigilDisplayModes = new ObservableCollection<string>();

        // Start of Constructors region

        #region Constructors

        public OverlayConfigViewModel(Action<OverlayConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<OverlayConfigViewModel>(closeHandler);
            OverlayConfigDoneCommand = new RelayCommand(OverlayConfigDoneExecute);

            // Init modes
            InitOverlayModes();
            InitSigilDisplayModes();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand OverlayConfigDoneCommand { get; }

        public ObservableCollection<string> OverlayMarkerModes { get => _overlayMarkerModes; set => _overlayMarkerModes = value; }
        public ObservableCollection<string> SigilDisplayModes { get => _sigilDisplayModes; set => _sigilDisplayModes = value; }

        public bool IsAspectDetectionEnabled
        {
            get => _settingsManager.Settings.IsAspectDetectionEnabled;
            set
            {
                _settingsManager.Settings.IsAspectDetectionEnabled = value;
                OnPropertyChanged(nameof(IsAspectDetectionEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsDungeonTiersEnabled
        {
            get => _settingsManager.Settings.DungeonTiers;
            set
            {
                _settingsManager.Settings.DungeonTiers = value;
                OnPropertyChanged(nameof(IsDungeonTiersEnabled));
                WeakReferenceMessenger.Default.Send(new DungeonTiersEnabledChangedMessage());

                _settingsManager.SaveSettings();
            }
        }

        public bool IsItemPowerLimitEnabled
        {
            get => _settingsManager.Settings.IsItemPowerLimitEnabled;
            set
            {
                _settingsManager.Settings.IsItemPowerLimitEnabled = value;
                OnPropertyChanged(nameof(IsItemPowerLimitEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsOverlayIconVisible
        {
            get => _settingsManager.Settings.ShowOverlayIcon;
            set
            {
                _settingsManager.Settings.ShowOverlayIcon = value;
                OnPropertyChanged(nameof(IsOverlayIconVisible));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsTemperedAffixDetectionEnabled
        {
            get => _settingsManager.Settings.IsTemperedAffixDetectionEnabled;
            set
            {
                _settingsManager.Settings.IsTemperedAffixDetectionEnabled = value;
                OnPropertyChanged(nameof(IsTemperedAffixDetectionEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsTradeOverlayEnabled
        {
            get => _settingsManager.Settings.IsTradeOverlayEnabled;
            set
            {
                _settingsManager.Settings.IsTradeOverlayEnabled = value;
                OnPropertyChanged(nameof(IsTradeOverlayEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public int ItemPowerLimit
        {
            get => _settingsManager.Settings.ItemPowerLimit;
            set
            {
                _settingsManager.Settings.ItemPowerLimit = value;
                OnPropertyChanged(nameof(ItemPowerLimit));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayFontSize
        {
            get => _settingsManager.Settings.OverlayFontSize;
            set
            {
                _settingsManager.Settings.OverlayFontSize = value;
                OnPropertyChanged(nameof(OverlayFontSize));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayIconPosX
        {
            get => _settingsManager.Settings.OverlayIconPosX;
            set
            {
                _settingsManager.Settings.OverlayIconPosX = value;
                OnPropertyChanged(nameof(OverlayIconPosX));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayIconPosY
        {
            get => _settingsManager.Settings.OverlayIconPosY;
            set
            {
                _settingsManager.Settings.OverlayIconPosY = value;
                OnPropertyChanged(nameof(OverlayIconPosY));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayUpdateDelay
        {
            get => _settingsManager.Settings.OverlayUpdateDelay;
            set
            {
                _settingsManager.Settings.OverlayUpdateDelay = value;
                OnPropertyChanged(nameof(OverlayUpdateDelay));

                _settingsManager.SaveSettings();
            }
        }

        public int ScanHeight
        {
            get => _settingsManager.Settings.ScanHeight;
            set
            {
                _settingsManager.Settings.ScanHeight = value;
                OnPropertyChanged(nameof(ScanHeight));

                _settingsManager.SaveSettings();
            }
        }

        public int ScreenCaptureDelay
        {
            get => _settingsManager.Settings.ScreenCaptureDelay;
            set
            {
                _settingsManager.Settings.ScreenCaptureDelay = value;
                OnPropertyChanged(nameof(ScreenCaptureDelay));

                _settingsManager.SaveSettings();
            }
        }

        public string SelectedOverlayMarkerMode
        {
            get => _settingsManager.Settings.SelectedOverlayMarkerMode;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _settingsManager.Settings.SelectedOverlayMarkerMode = value;
                    OnPropertyChanged(nameof(SelectedOverlayMarkerMode));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public string SelectedSigilDisplayMode
        {
            get => _settingsManager.Settings.SelectedSigilDisplayMode;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _settingsManager.Settings.SelectedSigilDisplayMode = value;
                    OnPropertyChanged(nameof(SelectedSigilDisplayMode));

                    _settingsManager.SaveSettings();
                }
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void OverlayConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitOverlayModes()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                // TODO: When localising this modify the OverlayHandler as well.
                OverlayMarkerModes.Add("Show All");
                OverlayMarkerModes.Add("Hide Unwanted");
            });
        }

        private void InitSigilDisplayModes()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                // TODO: When localising this modify the AffixManager/OverlayHandler as well.
                SigilDisplayModes.Add("Whitelisting");
                SigilDisplayModes.Add("Blacklisting");
            });
        }

        #endregion
    }
}
