using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class UniqueConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public UniqueConfigViewModel(Action<UniqueConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<UniqueConfigViewModel>(closeHandler);
            SetColorsCommand = new RelayCommand(SetColorsExecute);
            SetMultiBuildCommand = new RelayCommand(SetMultiBuildExecute);
            UniqueConfigDoneCommand = new RelayCommand(UniqueConfigDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand SetColorsCommand { get; }
        public ICommand SetMultiBuildCommand { get; }
        public ICommand UniqueConfigDoneCommand { get; }

        public bool IsMultiBuildModeEnabled
        {
            get => _settingsManager.Settings.IsMultiBuildModeEnabled;
            set
            {
                _settingsManager.Settings.IsMultiBuildModeEnabled = value;
                OnPropertyChanged(nameof(IsMultiBuildModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsUniqueDetectionEnabled
        {
            get => _settingsManager.Settings.IsUniqueDetectionEnabled;
            set
            {
                _settingsManager.Settings.IsUniqueDetectionEnabled = value;
                OnPropertyChanged(nameof(IsUniqueDetectionEnabled));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

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

        private async void SetMultiBuildExecute()
        {
            var multiBuildConfigDialog = new CustomDialog() { Title = "Multi build config" };
            var dataContext = new MultiBuildConfigViewModel(async instance =>
            {
                await multiBuildConfigDialog.WaitUntilUnloadedAsync();
            });
            multiBuildConfigDialog.Content = new MultiBuildConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, multiBuildConfigDialog);
            await multiBuildConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
        }

        private void UniqueConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
