using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Interfaces;
using D4Companion.Messages;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class SigilConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _sigilDisplayModes = new ObservableCollection<string>();

        // Start of Constructors region

        #region Constructors

        public SigilConfigViewModel(Action<SigilConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<SigilConfigViewModel>(closeHandler);
            SetMultiBuildCommand = new RelayCommand(SetMultiBuildExecute);
            SigilConfigDoneCommand = new RelayCommand(SigilConfigDoneExecute);

            // Init modes
            InitSigilDisplayModes();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand SetMultiBuildCommand { get; }
        public ICommand SigilConfigDoneCommand { get; }

        public ObservableCollection<string> SigilDisplayModes { get => _sigilDisplayModes; set => _sigilDisplayModes = value; }

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
