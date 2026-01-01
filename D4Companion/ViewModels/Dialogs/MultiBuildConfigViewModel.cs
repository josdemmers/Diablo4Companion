using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class MultiBuildConfigViewModel : ObservableObject
    {
        private readonly IAffixManager _affixManager;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<AffixPreset> _affixPresets = new();
        private ObservableCollection<MultiBuild> _multiBuildList = new();
        private AffixPreset _selectedAffixPreset = new AffixPreset();

        // Start of Constructors region

        #region Constructors

        public MultiBuildConfigViewModel(Action<MultiBuildConfigViewModel?> closeHandler)
        {
            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            AddBuildCommand = new RelayCommand(AddBuildExecute, CanAddBuildExecute);
            CloseCommand = new RelayCommand<MultiBuildConfigViewModel>(closeHandler);
            MultiBuildConfigDoneCommand = new RelayCommand(MultiBuildConfigDoneExecute);
            RemoveBuildCommand = new RelayCommand<object>(RemoveBuildExecute);
            SetColorBuildCommand = new RelayCommand<object>(SetColorBuildExecute);

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

        public ICommand AddBuildCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand MultiBuildConfigDoneCommand { get; }
        public ICommand RemoveBuildCommand { get; }
        public ICommand SetColorBuildCommand { get; }

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
                OnPropertyChanged(nameof(SelectedAffixPreset));
                ((RelayCommand)AddBuildCommand).NotifyCanExecuteChanged();
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

            ((RelayCommand)AddBuildCommand).NotifyCanExecuteChanged();
        }

        private void MultiBuildConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private void RemoveBuildExecute(object? build)
        {
            if (build == null) return;

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

            ((RelayCommand)AddBuildCommand).NotifyCanExecuteChanged();
        }

        private async void SetColorBuildExecute(object? build)
        {
            if (build == null) return;

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
