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
    public class AffixConfigViewModel : ObservableObject
    {
        private readonly IAffixManager _affixManager;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public AffixConfigViewModel(Action<AffixConfigViewModel?> closeHandler)
        {
            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            AffixConfigDoneCommand = new RelayCommand(AffixConfigDoneExecute);
            CloseCommand = new RelayCommand<AffixConfigViewModel>(closeHandler);
            ResetMinimalAffixValuesCommand = new RelayCommand(ResetMinimalAffixValuesExecute);
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
        public ICommand AffixConfigDoneCommand { get; }
        public ICommand ResetMinimalAffixValuesCommand { get; }
        public ICommand SetMultiBuildCommand { get; }
        public ICommand SetColorsCommand { get; }

        public bool IsMinimalAffixValueFilterEnabled
        {
            get => _settingsManager.Settings.IsMinimalAffixValueFilterEnabled;
            set
            {
                _settingsManager.Settings.IsMinimalAffixValueFilterEnabled = value;
                OnPropertyChanged(nameof(IsMinimalAffixValueFilterEnabled));

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

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AffixConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private void ResetMinimalAffixValuesExecute()
        {
            _dialogCoordinator.ShowMessageAsync(this, $"Reset", $"Are you sure you want to reset the minimal affix values?", MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    _affixManager.ResetMinimalAffixValues();
                }
            });
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
