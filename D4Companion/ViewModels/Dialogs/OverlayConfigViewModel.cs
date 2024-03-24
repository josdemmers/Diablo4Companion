using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace D4Companion.ViewModels.Dialogs
{
    public class OverlayConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _overlayMarkerModes = new ObservableCollection<string>();
        private ObservableCollection<string> _sigilDisplayModes = new ObservableCollection<string>();

        // Start of Constructors region

        #region Constructors

        public OverlayConfigViewModel(Action<OverlayConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<OverlayConfigViewModel>(closeHandler);
            OverlayConfigDoneCommand = new DelegateCommand(OverlayConfigDoneExecute);

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

        public DelegateCommand<OverlayConfigViewModel> CloseCommand { get; }
        public DelegateCommand OverlayConfigDoneCommand { get; }

        public ObservableCollection<string> OverlayMarkerModes { get => _overlayMarkerModes; set => _overlayMarkerModes = value; }
        public ObservableCollection<string> SigilDisplayModes { get => _sigilDisplayModes; set => _sigilDisplayModes = value; }

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

        public bool IsItemPowerLimitEnabled
        {
            get => _settingsManager.Settings.IsItemPowerLimitEnabled;
            set
            {
                _settingsManager.Settings.IsItemPowerLimitEnabled = value;
                RaisePropertyChanged(nameof(IsItemPowerLimitEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public int ItemPowerLimit
        {
            get => _settingsManager.Settings.ItemPowerLimit;
            set
            {
                _settingsManager.Settings.ItemPowerLimit = value;
                RaisePropertyChanged(nameof(ItemPowerLimit));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayFontSize
        {
            get => _settingsManager.Settings.OverlayFontSize;
            set
            {
                _settingsManager.Settings.OverlayFontSize = value;
                RaisePropertyChanged(nameof(OverlayFontSize));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayIconPosX
        {
            get => _settingsManager.Settings.OverlayIconPosX;
            set
            {
                _settingsManager.Settings.OverlayIconPosX = value;
                RaisePropertyChanged(nameof(OverlayIconPosX));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayIconPosY
        {
            get => _settingsManager.Settings.OverlayIconPosY;
            set
            {
                _settingsManager.Settings.OverlayIconPosY = value;
                RaisePropertyChanged(nameof(OverlayIconPosY));

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
                    RaisePropertyChanged(nameof(SelectedOverlayMarkerMode));

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
                    RaisePropertyChanged(nameof(SelectedSigilDisplayMode));

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
