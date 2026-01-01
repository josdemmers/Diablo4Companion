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
    public class AspectConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public AspectConfigViewModel(Action<AspectConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            AspectConfigDoneCommand = new RelayCommand(AspectConfigDoneExecute);
            CloseCommand = new RelayCommand<AspectConfigViewModel>(closeHandler);
            SetColorsCommand = new RelayCommand(SetColorsExecute);
            SetMultiBuildCommand = new RelayCommand(SetMultiBuildExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand AspectConfigDoneCommand { get; }
        public ICommand SetColorsCommand { get; }
        public ICommand SetMultiBuildCommand { get; }

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

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AspectConfigDoneExecute()
        {
            CloseCommand.Execute(this);
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

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
