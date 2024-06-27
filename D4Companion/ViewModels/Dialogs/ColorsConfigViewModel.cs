using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class ColorsConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public ColorsConfigViewModel(Action<ColorsConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<ColorsConfigViewModel>(closeHandler);
            ColorsConfigDoneCommand = new DelegateCommand(ColorsConfigDoneExecute);
            SetAffixColorCommand = new DelegateCommand<string>(SetAffixColorExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<ColorsConfigViewModel> CloseCommand { get; }
        public DelegateCommand ColorsConfigDoneCommand { get; }
        public DelegateCommand<string> SetAffixColorCommand { get; }

        public Color DefaultColorGreater
        {
            get => _settingsManager.Settings.DefaultColorGreater;
            set
            {
                _settingsManager.Settings.DefaultColorGreater = value;
                RaisePropertyChanged(nameof(DefaultColorGreater));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorImplicit
        {
            get => _settingsManager.Settings.DefaultColorImplicit;
            set
            {
                _settingsManager.Settings.DefaultColorImplicit = value;
                RaisePropertyChanged(nameof(DefaultColorImplicit));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorNormal
        {
            get => _settingsManager.Settings.DefaultColorNormal;
            set
            {
                _settingsManager.Settings.DefaultColorNormal = value;
                RaisePropertyChanged(nameof(DefaultColorNormal));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorTempered
        {
            get => _settingsManager.Settings.DefaultColorTempered;
            set
            {
                _settingsManager.Settings.DefaultColorTempered = value;
                RaisePropertyChanged(nameof(DefaultColorTempered));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorAspects
        {
            get => _settingsManager.Settings.DefaultColorAspects;
            set
            {
                _settingsManager.Settings.DefaultColorAspects = value;
                RaisePropertyChanged(nameof(DefaultColorAspects));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ColorsConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private async void SetAffixColorExecute(string affixType)
        {
            Color currentColor = affixType.Equals("Implicit") ? DefaultColorImplicit :
                affixType.Equals("Normal") ? DefaultColorNormal :
                affixType.Equals("Greater") ? DefaultColorGreater :
                affixType.Equals("Tempered") ? DefaultColorTempered :
                affixType.Equals("Aspects") ? DefaultColorAspects : DefaultColorNormal;

            var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
            var dataContext = new SetAffixTypeColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, currentColor);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();

            switch (affixType)
            {
                case "Implicit":
                    DefaultColorImplicit = dataContext.SelectedColor.Value;
                    break;
                case "Normal":
                    DefaultColorNormal = dataContext.SelectedColor.Value;
                    break;
                case "Greater":
                    DefaultColorGreater = dataContext.SelectedColor.Value;
                    break;
                case "Tempered":
                    DefaultColorTempered = dataContext.SelectedColor.Value;
                    break;
                case "Aspects":
                    DefaultColorAspects = dataContext.SelectedColor.Value;
                    break;
                default:
                    break;
            }

            _settingsManager.SaveSettings();
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
