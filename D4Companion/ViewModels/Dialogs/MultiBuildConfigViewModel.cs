using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class MultiBuildConfigViewModel : BindableBase
    {
        private readonly IAffixManager _affixManager;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<AffixPreset> _affixPresets = new();
        private AffixPreset _selectedAffixPreset = new AffixPreset();

        // Start of Constructors region

        #region Constructors

        public MultiBuildConfigViewModel(Action<MultiBuildConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            AddBuildCommand = new DelegateCommand(AddBuildExecute, CanAddBuildExecute);
            CloseCommand = new DelegateCommand<MultiBuildConfigViewModel>(closeHandler);
            MultiBuildConfigDoneCommand = new DelegateCommand(MultiBuildConfigDoneExecute);
            RemoveBuildCommand = new DelegateCommand<string>(RemoveBuildExecute);
            SetColorBuildCommand = new DelegateCommand<string>(SetColorBuildExecute);

            // Load affix presets
            UpdateAffixPresets();
        }

        private bool CanAddBuildExecute()
        {
            bool result = !SelectedAffixPreset.Name.Equals(NameBuild1) &&
                !SelectedAffixPreset.Name.Equals(NameBuild2) &&
                !SelectedAffixPreset.Name.Equals(NameBuild3);

            if (!result) return result;

            result = string.IsNullOrWhiteSpace(NameBuild1) ||
                string.IsNullOrWhiteSpace(NameBuild2) ||
                string.IsNullOrWhiteSpace(NameBuild3);

            return result;
        }

        private void AddBuildExecute()
        {
            if(string.IsNullOrWhiteSpace(NameBuild1))
            {
                NameBuild1 = SelectedAffixPreset.Name;
            }
            else if(string.IsNullOrWhiteSpace(NameBuild2))
            {
                NameBuild2 = SelectedAffixPreset.Name;
            }
            else if (string.IsNullOrWhiteSpace(NameBuild3))
            {
                NameBuild3 = SelectedAffixPreset.Name;
            }

            AddBuildCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }

        public DelegateCommand AddBuildCommand { get; }
        public DelegateCommand<MultiBuildConfigViewModel> CloseCommand { get; }
        public DelegateCommand MultiBuildConfigDoneCommand { get; }
        public DelegateCommand<string> RemoveBuildCommand { get; }
        public DelegateCommand<string> SetColorBuildCommand { get; }

        public Color ColorBuild1
        {
            get => _settingsManager.Settings.MultiBuildColor1;
            set
            {
                _settingsManager.Settings.MultiBuildColor1 = value;
                RaisePropertyChanged(nameof(ColorBuild1));

                _settingsManager.SaveSettings();
            }
        }

        public Color ColorBuild2
        {
            get => _settingsManager.Settings.MultiBuildColor2;
            set
            {
                _settingsManager.Settings.MultiBuildColor2 = value;
                RaisePropertyChanged(nameof(ColorBuild2));

                _settingsManager.SaveSettings();
            }
        }

        public Color ColorBuild3
        {
            get => _settingsManager.Settings.MultiBuildColor3;
            set
            {
                _settingsManager.Settings.MultiBuildColor3 = value;
                RaisePropertyChanged(nameof(ColorBuild3));

                _settingsManager.SaveSettings();
            }
        }

        public string NameBuild1
        {
            get => _settingsManager.Settings.MultiBuildName1;
            set
            {
                _settingsManager.Settings.MultiBuildName1 = value;
                RaisePropertyChanged(nameof(NameBuild1));

                _settingsManager.SaveSettings();
            }
        }

        public string NameBuild2
        {
            get => _settingsManager.Settings.MultiBuildName2;
            set
            {
                _settingsManager.Settings.MultiBuildName2 = value;
                RaisePropertyChanged(nameof(NameBuild2));

                _settingsManager.SaveSettings();
            }
        }

        public string NameBuild3
        {
            get => _settingsManager.Settings.MultiBuildName3;
            set
            {
                _settingsManager.Settings.MultiBuildName3 = value;
                RaisePropertyChanged(nameof(NameBuild3));

                _settingsManager.SaveSettings();
            }
        }

        public AffixPreset SelectedAffixPreset
        {
            get => _selectedAffixPreset;
            set
            {
                _selectedAffixPreset = value;
                if (value == null)
                {
                    _selectedAffixPreset = new AffixPreset();
                }
                RaisePropertyChanged(nameof(SelectedAffixPreset));
                AddBuildCommand?.RaiseCanExecuteChanged();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void MultiBuildConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private void RemoveBuildExecute(string buildNr)
        {
            switch (buildNr)
            {
                case "1":
                    NameBuild1 = string.Empty;
                    break;
                case "2":
                    NameBuild2 = string.Empty;
                    break;
                case "3":
                    NameBuild3 = string.Empty;
                    break;
                default:
                    break;
            }

            AddBuildCommand?.RaiseCanExecuteChanged();
        }

        private async void SetColorBuildExecute(string buildNr)
        {
            Color currentColor = buildNr.Equals("1") ? ColorBuild1 :
                buildNr.Equals("2") ? ColorBuild2 : ColorBuild3;

            var setAffixColorDialog = new CustomDialog() { Title = "Set build color" };
            var dataContext = new SetAffixTypeColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, currentColor);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();

            switch (buildNr)
            {
                case "1":
                    ColorBuild1 = dataContext.SelectedColor.Value;
                    break;
                case "2":
                    ColorBuild2 = dataContext.SelectedColor.Value;
                    break;
                case "3":
                    ColorBuild3 = dataContext.SelectedColor.Value;
                    break;
                default:
                    break;
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void UpdateAffixPresets()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                AffixPresets.Clear();
                AffixPresets.AddRange(_affixManager.AffixPresets);
                if (AffixPresets.Any())
                {
                    SelectedAffixPreset = AffixPresets[0];
                }
            });
        }

        #endregion
    }
}
