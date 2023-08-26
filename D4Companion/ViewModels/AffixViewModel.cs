using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Services;
using D4Companion.ViewModels.Dialogs;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.ViewModels
{
    public class AffixViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<AffixInfo> _affixes = new ObservableCollection<AffixInfo>();
        private ObservableCollection<AffixPresetV2> _affixPresets = new ObservableCollection<AffixPresetV2>();
        private ObservableCollection<AspectInfo> _aspects = new ObservableCollection<AspectInfo>();
        private ObservableCollection<ItemAffixV2> _selectedAffixes = new ObservableCollection<ItemAffixV2>();

        private string _affixPresetName = string.Empty;
        private string _affixTextFilter = string.Empty;
        private int? _badgeCount = null;
        private bool _isAffixOverlayEnabled = false;
        private AffixPresetV2 _selectedAffixPreset = new AffixPresetV2();
        private bool _toggleCore = true;
        private bool _toggleBarbarian = false;
        private bool _toggleDruid = false;
        private bool _toggleNecromancer = false;
        private bool _toggleRogue = false;
        private bool _toggleSorcerer = false;

        // Start of Constructors region

        #region Constructors

        public AffixViewModel(IEventAggregator eventAggregator, ILogger<AffixViewModel> logger, IAffixManager affixManager, IDialogCoordinator dialogCoordinator, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Subscribe(HandleSelectedAffixesChangedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayKeyBindingEvent>().Subscribe(HandleToggleOverlayKeyBindingEvent);

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _dialogCoordinator = dialogCoordinator;
            _settingsManager = settingsManager;

            // Init View commands
            AddAffixPresetNameCommand = new DelegateCommand(AddAffixPresetNameExecute, CanAddAffixPresetNameExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            SetAffixCommand = new DelegateCommand<AffixInfo>(SetAffixExecute);

            // Init filter views
            CreateItemAffixesFilteredView();
            CreateItemAspectsFilteredView();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }
        public ObservableCollection<AffixPresetV2> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<AspectInfo> Aspects { get => _aspects; set => _aspects = value; }
        public ObservableCollection<ItemAffixV2> SelectedAffixes { get => _selectedAffixes; set => _selectedAffixes = value; }
        public ListCollectionView? AffixesFiltered { get; private set; }
        public ListCollectionView? AspectsFiltered { get; private set; }

        public DelegateCommand AddAffixPresetNameCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<AffixInfo> SetAffixCommand { get; }

        public string AffixPresetName
        {
            get => _affixPresetName;
            set
            {
                SetProperty(ref _affixPresetName, value, () => { RaisePropertyChanged(nameof(AffixPresetName)); });
                AddAffixPresetNameCommand?.RaiseCanExecuteChanged();
            }
        }

        public string AffixTextFilter
        {
            get => _affixTextFilter;
            set
            {
                SetProperty(ref _affixTextFilter, value, () => { RaisePropertyChanged(nameof(AffixTextFilter)); });
                AffixesFiltered?.Refresh();
                AspectsFiltered?.Refresh();
            }
        }
        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public bool IsAffixOverlayEnabled
        {
            get => _isAffixOverlayEnabled;
            set
            {
                _isAffixOverlayEnabled = value;
                RaisePropertyChanged(nameof(IsAffixOverlayEnabled));
                _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Publish(new ToggleOverlayFromGUIEventParams { IsEnabled = value });
            }
        }

        public bool IsAffixPresetSelected
        {
            get
            {
                return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
            }
        }

        public AffixPresetV2 SelectedAffixPreset
        {
            get => _selectedAffixPreset;
            set
            {
                _selectedAffixPreset = value;
                RaisePropertyChanged(nameof(SelectedAffixPreset));
                RaisePropertyChanged(nameof(IsAffixPresetSelected));
                RemoveAffixPresetNameCommand?.RaiseCanExecuteChanged();
                if (value != null)
                {
                    _settingsManager.Settings.SelectedAffixName = value.Name;
                    _settingsManager.SaveSettings();
                }
                else
                {
                    _selectedAffixPreset = new AffixPresetV2();
                }
                UpdateSelectedAffixes();
            }
        }

        public bool ToggleCore
        {
            get => _toggleCore; set
            {
                _toggleCore = value;

                if (value) 
                {
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleCore));
            }
        }

        /// <summary>
        /// Reset filter when all category toggles are false.
        /// </summary>
        private void CheckResetAffixFilter()
        {
            if (!ToggleCore && !ToggleBarbarian && !ToggleDruid && !ToggleNecromancer && !ToggleRogue && !ToggleSorcerer) 
            {
                AffixesFiltered?.Refresh();
                AspectsFiltered?.Refresh();
            }
        }

        public bool ToggleBarbarian
        {
            get => _toggleBarbarian;
            set
            {
                _toggleBarbarian = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleBarbarian));
            }
        }

        public bool ToggleDruid
        {
            get => _toggleDruid; set
            {
                _toggleDruid = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleDruid));
            }
        }

        public bool ToggleNecromancer
        {
            get => _toggleNecromancer;
            set
            {
                _toggleNecromancer = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleNecromancer));
            }
        }

        public bool ToggleRogue
        {
            get => _toggleRogue;
            set
            {
                _toggleRogue = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleRogue));
            }
        }

        public bool ToggleSorcerer
        {
            get => _toggleSorcerer;
            set
            {
                _toggleSorcerer = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;

                    AffixesFiltered?.Refresh();
                    AspectsFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleSorcerer));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleAffixPresetAddedEvent()
        {
            UpdateAffixPresets();

            // Select added preset
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(AffixPresetName));
            if (preset != null)
            {
                SelectedAffixPreset = preset;
            }

            // Clear preset name
            AffixPresetName = string.Empty;
        }

        private void HandleAffixPresetRemovedEvent()
        {
            UpdateAffixPresets();

            // Select first preset
            if (AffixPresets.Count > 0)
            {
                SelectedAffixPreset = AffixPresets[0];
            }
        }

        private void HandleApplicationLoadedEvent()
        {
            // Load affix and aspect gamedata
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Affixes.Clear();
                Affixes.AddRange(_affixManager.Affixes);

                Aspects.Clear();
                Aspects.AddRange(_affixManager.Aspects);
            });

            // Load affix presets
            UpdateAffixPresets();

            // Load selected affixes
            UpdateSelectedAffixes();
        }

        private void HandleSelectedAffixesChangedEvent()
        {
            UpdateSelectedAffixes();
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsAffixOverlayEnabled = toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayKeyBindingEvent()
        {
            IsAffixOverlayEnabled = !IsAffixOverlayEnabled;
        }

        private async void SetAffixExecute(AffixInfo affixInfo)
        {
            if (affixInfo != null)
            {
                var setAffixDialog = new CustomDialog() { Title = "Set affix" };
                var dataContext = new SetAffixViewModel(async instance =>
                {
                    await setAffixDialog.WaitUntilUnloadedAsync();
                }, affixInfo);
                setAffixDialog.Content = new SetAffixView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixDialog);
                await setAffixDialog.WaitUntilUnloadedAsync();
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        private bool CanAddAffixPresetNameExecute()
        {
            return !string.IsNullOrWhiteSpace(AffixPresetName) &&
                !_affixPresets.Any(preset => preset.Name.Equals(AffixPresetName));
        }

        private void AddAffixPresetNameExecute()
        {
            _affixManager.AddAffixPreset(new AffixPresetV2
            {
                Name = AffixPresetName
            });
        }

        private void CreateItemAffixesFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AffixesFiltered = new ListCollectionView(Affixes)
                {
                    Filter = FilterAffixes
                };
            });
        }

        private bool FilterAffixes(object affixObj)
        {
            var allowed = true;
            if (affixObj == null) return false;

            AffixInfo affixInfo = (AffixInfo)affixObj;

            if (!affixInfo.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (ToggleCore)
            {
                allowed = affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleBarbarian)
            {
                allowed = affixInfo.AllowedForPlayerClass[2] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleDruid)
            {
                allowed = affixInfo.AllowedForPlayerClass[1] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleNecromancer)
            {
                allowed = affixInfo.AllowedForPlayerClass[4] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleRogue)
            {
                allowed = affixInfo.AllowedForPlayerClass[3] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleSorcerer)
            {
                allowed = affixInfo.AllowedForPlayerClass[0] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }

            return allowed;
        }

        private void CreateItemAspectsFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AspectsFiltered = new ListCollectionView(Aspects)
                {
                    Filter = FilterAspects
                };
            });
        }

        private bool FilterAspects(object aspectObj)
        {
            var allowed = true;
            if (aspectObj == null) return false;

            AspectInfo aspectInfo = (AspectInfo)aspectObj;

            if (!aspectInfo.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !aspectInfo.Name.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (ToggleCore)
            {
                allowed = aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleBarbarian)
            {
                allowed = aspectInfo.AllowedForPlayerClass[2] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleDruid)
            {
                allowed = aspectInfo.AllowedForPlayerClass[1] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleNecromancer)
            {
                allowed = aspectInfo.AllowedForPlayerClass[4] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleRogue)
            {
                allowed = aspectInfo.AllowedForPlayerClass[3] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleSorcerer)
            {
                allowed = aspectInfo.AllowedForPlayerClass[0] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1);
            }

            return allowed;
        }

        private void UpdateAffixPresets()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                AffixPresets.Clear();
                AffixPresets.AddRange(_affixManager.AffixPresets);
                if (AffixPresets.Any())
                {
                    // Load settings
                    var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixName));
                    if (preset != null)
                    {
                        SelectedAffixPreset = preset;
                    }
                }
            });
            AddAffixPresetNameCommand?.RaiseCanExecuteChanged();
        }

        private void UpdateSelectedAffixes()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedAffixes.Clear();
                if (SelectedAffixPreset != null)
                {
                    SelectedAffixes.AddRange(SelectedAffixPreset.ItemAffixes);
                }
            });
        }

        private bool CanRemoveAffixPresetNameExecute()
        {
            return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
        }

        private void RemoveAffixPresetNameExecute()
        {
            _dialogCoordinator.ShowMessageAsync(this, $"Delete", $"Are you sure you want to delete preset \"{SelectedAffixPreset.Name}\"", MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    _logger.LogInformation($"Deleted preset \"{SelectedAffixPreset.Name}\"");
                    _affixManager.RemoveAffixPreset(SelectedAffixPreset);
                }
            });
        }

        #endregion
    }
}
