﻿using D4Companion.Events;
using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace D4Companion.ViewModels.Dialogs
{
    public class SigilConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _sigilDisplayModes = new ObservableCollection<string>();

        // Start of Constructors region

        #region Constructors

        public SigilConfigViewModel(Action<SigilConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            SigilConfigDoneCommand = new DelegateCommand(SigilConfigDoneExecute);
            CloseCommand = new DelegateCommand<SigilConfigViewModel>(closeHandler);

            // Init modes
            InitSigilDisplayModes();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<SigilConfigViewModel> CloseCommand { get; }
        public DelegateCommand SigilConfigDoneCommand { get; }

        public ObservableCollection<string> SigilDisplayModes { get => _sigilDisplayModes; set => _sigilDisplayModes = value; }

        public bool IsDungeonTiersEnabled
        {
            get => _settingsManager.Settings.DungeonTiers;
            set
            {
                _settingsManager.Settings.DungeonTiers = value;
                RaisePropertyChanged(nameof(IsDungeonTiersEnabled));
                _eventAggregator.GetEvent<SelectedSigilDungeonTierChangedEvent>().Publish();

                _settingsManager.SaveSettings();
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

        private void SigilConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

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