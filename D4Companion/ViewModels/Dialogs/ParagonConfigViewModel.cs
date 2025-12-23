using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class ParagonConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public ParagonConfigViewModel(Action<ParagonConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<ParagonConfigViewModel>(closeHandler);
            ParagonConfigDoneCommand = new RelayCommand(ParagonConfigDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand ParagonConfigDoneCommand { get; }

        public bool IsCollapsedParagonboardEnabled
        {
            get => _settingsManager.Settings.IsCollapsedParagonboardEnabled;
            set
            {
                _settingsManager.Settings.IsCollapsedParagonboardEnabled = value;
                OnPropertyChanged(nameof(IsCollapsedParagonboardEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public int ParagonBorderSize
        {
            get => _settingsManager.Settings.ParagonBorderSize;
            set
            {
                _settingsManager.Settings.ParagonBorderSize = value;
                OnPropertyChanged(nameof(ParagonBorderSize));

                _settingsManager.SaveSettings();
            }
        }

        public int ParagonLeftOffsetCollapsed
        {
            get => _settingsManager.Settings.ParagonLeftOffsetCollapsed;
            set
            {
                _settingsManager.Settings.ParagonLeftOffsetCollapsed = value;
                OnPropertyChanged(nameof(ParagonLeftOffsetCollapsed));

                _settingsManager.SaveSettings();
            }
        }

        public int ParagonNodeSize
        {
            get => _settingsManager.Settings.ParagonNodeSize;
            set
            {
                _settingsManager.Settings.ParagonNodeSize = value;
                OnPropertyChanged(nameof(ParagonNodeSize));

                _settingsManager.SaveSettings();
            }
        }

        public int ParagonNodeSizeCollapsed
        {
            get => _settingsManager.Settings.ParagonNodeSizeCollapsed;
            set
            {
                _settingsManager.Settings.ParagonNodeSizeCollapsed = value;
                OnPropertyChanged(nameof(ParagonNodeSizeCollapsed));

                _settingsManager.SaveSettings();
            }
        }

        public int ParagonTopOffsetCollapsed
        {
            get => _settingsManager.Settings.ParagonTopOffsetCollapsed;
            set
            {
                _settingsManager.Settings.ParagonTopOffsetCollapsed = value;
                OnPropertyChanged(nameof(ParagonTopOffsetCollapsed));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ParagonConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
