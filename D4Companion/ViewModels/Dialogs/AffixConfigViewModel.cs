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
        private readonly IAffixManager _affixManager;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public AffixConfigViewModel(Action<AffixConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            AffixConfigDoneCommand = new DelegateCommand(AffixConfigDoneExecute);
            CloseCommand = new DelegateCommand<AffixConfigViewModel>(closeHandler);
            ResetMinimalAffixValuesCommand = new DelegateCommand(ResetMinimalAffixValuesExecute);
            SetColorsCommand = new DelegateCommand(SetColorsExecute);
            SetMultiBuildCommand = new DelegateCommand(SetMultiBuildExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<AffixConfigViewModel> CloseCommand { get; }
        public DelegateCommand AffixConfigDoneCommand { get; }
        public DelegateCommand ResetMinimalAffixValuesCommand { get; }
        public DelegateCommand SetMultiBuildCommand { get; }
        public DelegateCommand SetColorsCommand { get; }

        public bool IsMinimalAffixValueFilterEnabled
        {
            get => _settingsManager.Settings.IsMinimalAffixValueFilterEnabled;
            set
            {
                _settingsManager.Settings.IsMinimalAffixValueFilterEnabled = value;
                RaisePropertyChanged(nameof(IsMinimalAffixValueFilterEnabled));

                _settingsManager.SaveSettings();
            }
        }

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
