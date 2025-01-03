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
        private ObservableCollection<MultiBuild> _multiBuildList = new();
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
            RemoveBuildCommand = new DelegateCommand<object>(RemoveBuildExecute);
            SetColorBuildCommand = new DelegateCommand<object>(SetColorBuildExecute);

            // Load affix presets
            UpdateAffixPresets();
            UpdateMultiBuildList();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<MultiBuild> MultiBuildList { get => _multiBuildList; set => _multiBuildList = value; }

        public DelegateCommand AddBuildCommand { get; }
        public DelegateCommand<MultiBuildConfigViewModel> CloseCommand { get; }
        public DelegateCommand MultiBuildConfigDoneCommand { get; }
        public DelegateCommand<object> RemoveBuildCommand { get; }
        public DelegateCommand<object> SetColorBuildCommand { get; }

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

        private bool CanAddBuildExecute()
        {
            return !MultiBuildList.Any(b => b.Name.Equals(SelectedAffixPreset.Name));
        }

        private void AddBuildExecute()
        {
            // Update indexes
            for (int i = 0; i < MultiBuildList.Count; i++)
            {
                MultiBuildList[i].Index = i;
            }

            MultiBuildList.Add(new MultiBuild
            {
                Color = Colors.Green,
                Index = MultiBuildList.Count,
                Name = SelectedAffixPreset.Name,

            });

            _settingsManager.Settings.MultiBuildList.Clear();
            _settingsManager.Settings.MultiBuildList.AddRange(MultiBuildList);

            AddBuildCommand?.RaiseCanExecuteChanged();
        }

        private void MultiBuildConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private void RemoveBuildExecute(object build)
        {
            MultiBuild multiBuild = (MultiBuild)build;
            MultiBuildList.Remove(multiBuild);

            // Update indexes
            for (int i = 0; i < MultiBuildList.Count; i++)
            {
                MultiBuildList[i].Index = i;
            }

            _settingsManager.Settings.MultiBuildList.Clear();
            _settingsManager.Settings.MultiBuildList.AddRange(MultiBuildList);
            _settingsManager.SaveSettings();

            AddBuildCommand?.RaiseCanExecuteChanged();
        }

        private async void SetColorBuildExecute(object build)
        {
            MultiBuild multiBuild = (MultiBuild)build;
            Color currentColor = multiBuild.Index < MultiBuildList.Count ? MultiBuildList[multiBuild.Index].Color : Colors.Green;

            var setAffixColorDialog = new CustomDialog() { Title = "Set build color" };
            var dataContext = new SetAffixTypeColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, currentColor);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();

            MultiBuildList[multiBuild.Index].Color = dataContext.SelectedColor.Value;
            _settingsManager.SaveSettings();
            UpdateMultiBuildList();
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

        private void UpdateMultiBuildList()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MultiBuildList.Clear();
                MultiBuildList.AddRange(_settingsManager.Settings.MultiBuildList);
            });
        }

        #endregion
    }
}
