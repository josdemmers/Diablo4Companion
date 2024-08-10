using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class AffixConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public AffixConfigViewModel(Action<AffixConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            AffixConfigDoneCommand = new DelegateCommand(AffixConfigDoneExecute);
            CloseCommand = new DelegateCommand<AffixConfigViewModel>(closeHandler);
            SetColorsCommand = new DelegateCommand(SetColorsExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<AffixConfigViewModel> CloseCommand { get; }
        public DelegateCommand AffixConfigDoneCommand { get; }
        public DelegateCommand SetColorsCommand { get; }

        public bool IsTemperedAffixDetectionEnabled
        {
            get => _settingsManager.Settings.IsTemperedAffixDetectionEnabled;
            set
            {
                _settingsManager.Settings.IsTemperedAffixDetectionEnabled = value;
                RaisePropertyChanged(nameof(IsTemperedAffixDetectionEnabled));

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

        #endregion
    }
}
