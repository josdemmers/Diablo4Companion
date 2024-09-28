using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class RuneConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public RuneConfigViewModel(Action<RuneConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<RuneConfigViewModel>(closeHandler);
            RuneConfigDoneCommand = new DelegateCommand(RuneConfigDoneExecute);
            SetColorsCommand = new DelegateCommand(SetColorsExecute);
            SetMultiBuildCommand = new DelegateCommand(SetMultiBuildExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<RuneConfigViewModel> CloseCommand { get; }
        public DelegateCommand RuneConfigDoneCommand { get; }
        public DelegateCommand SetColorsCommand { get; }
        public DelegateCommand SetMultiBuildCommand { get; }

        public bool IsMultiBuildModeEnabled
        {
            get => _settingsManager.Settings.IsMultiBuildModeEnabled;
            set
            {
                _settingsManager.Settings.IsMultiBuildModeEnabled = value;
                RaisePropertyChanged(nameof(IsMultiBuildModeEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsRuneDetectionEnabled
        {
            get => _settingsManager.Settings.IsRuneDetectionEnabled;
            set
            {
                _settingsManager.Settings.IsRuneDetectionEnabled = value;
                RaisePropertyChanged(nameof(IsRuneDetectionEnabled));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void RuneConfigDoneExecute()
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

        #endregion

        // Start of Methods region

        #region Methods

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
    }
}
