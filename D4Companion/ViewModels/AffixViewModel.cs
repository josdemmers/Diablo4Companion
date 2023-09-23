using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.ViewModels.Dialogs;
using D4Companion.ViewModels.Entities;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<AffixInfoVM> _affixes = new ObservableCollection<AffixInfoVM>();
        private ObservableCollection<AffixLanguage> _affixLanguages = new ObservableCollection<AffixLanguage>();
        private ObservableCollection<AffixPreset> _affixPresets = new ObservableCollection<AffixPreset>();
        private ObservableCollection<AspectInfoVM> _aspects = new ObservableCollection<AspectInfoVM>();
        private ObservableCollection<ItemAffix> _selectedAffixes = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedAspects = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedConsumables = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedSigils = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedSeasonalItems = new ObservableCollection<ItemAffix>();
        private ObservableCollection<SigilInfoVM> _sigils = new ObservableCollection<SigilInfoVM>();

        private string _affixPresetName = string.Empty;
        private string _affixTextFilter = string.Empty;
        private int? _badgeCount = null;
        private bool _isAffixOverlayEnabled = false;
        private AffixLanguage _selectedAffixLanguage = new AffixLanguage();
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private int _selectedTabIndex = 0;
        private bool _toggleCore = true;
        private bool _toggleBarbarian = false;
        private bool _toggleDruid = false;
        private bool _toggleNecromancer = false;
        private bool _toggleRogue = false;
        private bool _toggleSorcerer = false;
        private bool _toggleElixers = false;
        private bool _toggleDungeons = true;
        private bool _togglePositive = false;
        private bool _toggleMinor = false;
        private bool _toggleMajor = false;
        private bool _toggleCagedHearts = false;

        // Start of Constructors region

        #region Constructors

        public AffixViewModel(IEventAggregator eventAggregator, ILogger<AffixViewModel> logger, IAffixManager affixManager, 
            IDialogCoordinator dialogCoordinator, ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<ExperimentalConsumablesChangedEvent>().Subscribe(HandleExperimentalConsumablesChangedEvent);
            _eventAggregator.GetEvent<ExperimentalSeasonalChangedEvent>().Subscribe(HandleExperimentalSeasonalChangedEvent);
            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Subscribe(HandleSelectedAffixesChangedEvent);
            _eventAggregator.GetEvent<SelectedAspectsChangedEvent>().Subscribe(HandleSelectedAspectsChangedEvent);
            _eventAggregator.GetEvent<SelectedSigilsChangedEvent>().Subscribe(HandleSelectedSigilsChangedEvent);
            _eventAggregator.GetEvent<SwitchPresetKeyBindingEvent>().Subscribe(HandleSwitchPresetKeyBindingEvent);
            _eventAggregator.GetEvent<SystemPresetMappingChangedEvent>().Subscribe(HandleSystemPresetMappingChangedEvent);
            _eventAggregator.GetEvent<SystemPresetItemTypesLoadedEvent>().Subscribe(HandleSystemPresetItemTypesLoadedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayKeyBindingEvent>().Subscribe(HandleToggleOverlayKeyBindingEvent);
            
            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _dialogCoordinator = dialogCoordinator;
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init View commands
            AddAffixPresetNameCommand = new DelegateCommand(AddAffixPresetNameExecute, CanAddAffixPresetNameExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            RemoveAffixCommand = new DelegateCommand<ItemAffix>(RemoveAffixExecute);
            RemoveAspectCommand = new DelegateCommand<ItemAffix>(RemoveAspectExecute);
            RemoveSigilCommand = new DelegateCommand<ItemAffix>(RemoveSigilExecute);
            SetAffixCommand = new DelegateCommand<AffixInfoVM>(SetAffixExecute, CanSetAffixExecute);
            SetAffixColorCommand = new DelegateCommand<ItemAffix>(SetAffixColorExecute);
            SetAffixMappingCommand = new DelegateCommand<AffixInfoVM>(SetAffixMappingExecute);
            SetAspectCommand = new DelegateCommand<AspectInfoVM>(SetAspectExecute, CanSetAspectExecute);
            SetAspectColorCommand = new DelegateCommand<ItemAffix>(SetAspectColorExecute);
            SetAspectMappingCommand = new DelegateCommand<AspectInfoVM>(SetAspectMappingExecute);
            SetSigilCommand = new DelegateCommand<SigilInfoVM>(SetSigilExecute, CanSetSigilExecute);
            SetSigilMappingCommand = new DelegateCommand<SigilInfoVM>(SetSigilMappingExecute);

            // Init filter views
            CreateItemAffixesFilteredView();
            CreateItemAspectsFilteredView();
            CreateItemSigilsFilteredView();
            CreateSelectedAffixesHelmFilteredView();
            CreateSelectedAffixesChestFilteredView();
            CreateSelectedAffixesGlovesFilteredView();
            CreateSelectedAffixesPantsFilteredView();
            CreateSelectedAffixesBootsFilteredView();
            CreateSelectedAffixesAmuletFilteredView();
            CreateSelectedAffixesRingFilteredView();
            CreateSelectedAffixesWeaponFilteredView();
            CreateSelectedAffixesRangedFilteredView();
            CreateSelectedAffixesOffhandFilteredView();
            CreateSelectedAspectsFilteredView();

            // Init affix languages
            InitAffixlanguages();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfoVM> Affixes { get => _affixes; set => _affixes = value; }
        public ObservableCollection<AffixLanguage> AffixLanguages { get => _affixLanguages; set => _affixLanguages = value; }
        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<AspectInfoVM> Aspects { get => _aspects; set => _aspects = value; }
        public ObservableCollection<ItemAffix> SelectedAffixes { get => _selectedAffixes; set => _selectedAffixes = value; }
        public ObservableCollection<ItemAffix> SelectedAspects { get => _selectedAspects; set => _selectedAspects = value; }
        public ObservableCollection<ItemAffix> SelectedConsumables { get => _selectedConsumables; set => _selectedConsumables = value; }
        public ObservableCollection<ItemAffix> SelectedSigils { get => _selectedSigils; set => _selectedSigils = value; }
        public ObservableCollection<ItemAffix> SelectedSeasonalItems { get => _selectedSeasonalItems; set => _selectedSeasonalItems = value; }
        public ObservableCollection<SigilInfoVM> Sigils { get => _sigils; set => _sigils = value; }
        public ListCollectionView? AffixesFiltered { get; private set; }
        public ListCollectionView? AspectsFiltered { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredHelm { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredChest { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredGloves { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredPants { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredBoots { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredAmulet { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredRing { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredWeapon { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredRanged { get; private set; }
        public ListCollectionView? SelectedAffixesFilteredOffhand { get; private set; }
        public ListCollectionView? SelectedAspectsFiltered { get; private set; }
        public ListCollectionView? SigilsFiltered { get; private set; }

        public DelegateCommand AddAffixPresetNameCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<ItemAffix> RemoveAffixCommand { get; }
        public DelegateCommand<ItemAffix> RemoveAspectCommand { get; }
        public DelegateCommand<ItemAffix> RemoveSigilCommand { get; }
        public DelegateCommand<AffixInfoVM> SetAffixCommand { get; }
        public DelegateCommand<ItemAffix> SetAffixColorCommand { get; }
        public DelegateCommand<AffixInfoVM> SetAffixMappingCommand { get; }
        public DelegateCommand<AspectInfoVM> SetAspectCommand { get; }
        public DelegateCommand<ItemAffix> SetAspectColorCommand { get; }
        public DelegateCommand<AspectInfoVM> SetAspectMappingCommand { get; }
        public DelegateCommand<SigilInfoVM> SetSigilCommand { get; }
        public DelegateCommand<SigilInfoVM> SetSigilMappingCommand { get; }

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
                SigilsFiltered?.Refresh();
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

        public bool IsExperimentalConsumablesModeEnabled
        {
            get => _settingsManager.Settings.ExperimentalModeConsumables;
        }

        public bool IsExperimentalSeasonalModeEnabled
        {
            get => _settingsManager.Settings.ExperimentalModeSeasonal;
        }

        public bool IsAffixesTabActive
        {
            get => SelectedTabIndex == 0;
        }

        public bool IsAspectsTabActive
        {
            get => SelectedTabIndex == 1;
        }

        public bool IsConsumablesTabActive
        {
            get => SelectedTabIndex == 2;
        }

        public bool IsSigilsTabActive
        {
            get => SelectedTabIndex == 3;
        }

        public bool IsSeasonalTabActive
        {
            get => SelectedTabIndex == 4;
        }

        public bool IsItemTypeImageFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(string.Empty);
        }

        public bool IsItemTypeImageHelmFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Helm);            
        }

        public bool IsItemTypeImageChestFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Chest);
        }

        public bool IsItemTypeImageGlovesFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Gloves);
        }

        public bool IsItemTypeImagePantsFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Pants);
        }

        public bool IsItemTypeImageBootsFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Boots);
        }

        public bool IsItemTypeImageAmuletFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Amulet);
        }

        public bool IsItemTypeImageRingFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Ring);
        }

        public bool IsItemTypeImageWeaponFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Weapon);
        }

        public bool IsItemTypeImageRangedFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Ranged);
        }

        public bool IsItemTypeImageOffhandFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Offhand);
        }

        public bool IsItemTypeImageConsumableFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Consumable);
        }

        public bool IsItemTypeImageSigilFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Sigil);
        }

        public bool IsItemTypeImageSeasonalFound
        {
            get => _systemPresetManager.IsItemTypeImageFound(ItemTypeConstants.Seasonal);
        }

        public AffixLanguage SelectedAffixLanguage
        {
            get => _selectedAffixLanguage;
            set
            {
                _selectedAffixLanguage = value;
                RaisePropertyChanged(nameof(SelectedAffixLanguage));
                if (value != null)
                {
                    _settingsManager.Settings.SelectedAffixLanguage = value.Id;
                    _settingsManager.SaveSettings();

                    _eventAggregator.GetEvent<AffixLanguageChangedEvent>().Publish();

                    Affixes.Clear();
                    Affixes.AddRange(_affixManager.Affixes.Select(affixInfo => new AffixInfoVM(affixInfo)));

                    Aspects.Clear();
                    Aspects.AddRange(_affixManager.Aspects.Select(aspectInfo => new AspectInfoVM(aspectInfo)));

                    Sigils.Clear();
                    Sigils.AddRange(_affixManager.Sigils.Select(sigilInfo => new SigilInfoVM(sigilInfo)));

                    UpdateSelectedAffixes();
                    UpdateSelectedAspects();
                    UpdateSelectedSigils();
                }
            }
        }

        public AffixPreset SelectedAffixPreset
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
                    _settingsManager.Settings.SelectedAffixPreset = value.Name;
                    _settingsManager.SaveSettings();
                }
                else
                {
                    _selectedAffixPreset = new AffixPreset();
                }
                UpdateSelectedAffixes();
                UpdateSelectedAspects();
                UpdateSelectedSigils();
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged();

                RaisePropertyChanged(nameof(IsAffixesTabActive));
                RaisePropertyChanged(nameof(IsAspectsTabActive));
                RaisePropertyChanged(nameof(IsConsumablesTabActive));
                RaisePropertyChanged(nameof(IsSigilsTabActive));
                RaisePropertyChanged(nameof(IsSeasonalTabActive));
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

        /// <summary>
        /// Reset filter when all category toggles are false.
        /// </summary>
        private void CheckResetSigilFilter()
        {
            if (!ToggleDungeons && !TogglePositive && !ToggleMinor && !ToggleMajor)
            {
                SigilsFiltered?.Refresh();
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

        public bool ToggleElixers
        {
            get => _toggleElixers;
            set
            {
                _toggleElixers = value;
                RaisePropertyChanged();
            }
        }

        public bool ToggleDungeons
        {
            get => _toggleDungeons;
            set
            {
                _toggleDungeons = value;

                if (value)
                {
                    TogglePositive = false;
                    ToggleMinor = false;
                    ToggleMajor = false;

                    SigilsFiltered?.Refresh();
                }

                CheckResetSigilFilter();
                RaisePropertyChanged();
            }
        }

        public bool TogglePositive
        {
            get => _togglePositive;
            set
            {
                _togglePositive = value;

                if (value)
                {
                    ToggleDungeons = false;
                    ToggleMinor = false;
                    ToggleMajor = false;

                    SigilsFiltered?.Refresh();
                }

                CheckResetSigilFilter();
                RaisePropertyChanged();
            }
        }

        public bool ToggleMinor
        {
            get => _toggleMinor;
            set
            {
                _toggleMinor = value;

                if (value)
                {
                    ToggleDungeons = false;
                    TogglePositive = false;
                    ToggleMajor = false;

                    SigilsFiltered?.Refresh();
                }

                CheckResetSigilFilter();
                RaisePropertyChanged();
            }
        }

        public bool ToggleMajor
        {
            get => _toggleMajor;
            set
            {
                _toggleMajor = value;

                if (value)
                {
                    ToggleDungeons = false;
                    TogglePositive = false;
                    ToggleMinor = false;

                    SigilsFiltered?.Refresh();
                }

                CheckResetSigilFilter();
                RaisePropertyChanged();
            }
        }

        public bool ToggleCagedHearts
        {
            get => _toggleCagedHearts;
            set
            {
                _toggleCagedHearts = value;
                RaisePropertyChanged();
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
                Affixes.AddRange(_affixManager.Affixes.Select(affixInfo => new AffixInfoVM(affixInfo)));

                Aspects.Clear();
                Aspects.AddRange(_affixManager.Aspects.Select(aspectInfo => new AspectInfoVM(aspectInfo)));

                Sigils.Clear();
                Sigils.AddRange(_affixManager.Sigils.Select(sigilInfo => new SigilInfoVM(sigilInfo)));
            });

            // Load affix presets
            UpdateAffixPresets();

            // Load selected affixes
            UpdateSelectedAffixes();

            // Load selected aspects
            UpdateSelectedAspects();

            // Load selectes sigils
            UpdateSelectedSigils();
        }

        private void HandleExperimentalConsumablesChangedEvent()
        {
            RaisePropertyChanged(nameof(IsExperimentalConsumablesModeEnabled));
        }

        private void HandleExperimentalSeasonalChangedEvent()
        {
            RaisePropertyChanged(nameof(IsExperimentalSeasonalModeEnabled));
        }

        private void HandleSelectedAffixesChangedEvent()
        {
            UpdateSelectedAffixes();
        }

        private void HandleSelectedAspectsChangedEvent()
        {
            UpdateSelectedAspects();
        }

        private void HandleSelectedSigilsChangedEvent()
        {
            UpdateSelectedSigils();
        }

        private void HandleSwitchPresetKeyBindingEvent()
        {
            int affixIndex = 0;
            if (SelectedAffixPreset != null)
            {
                affixIndex = AffixPresets.IndexOf(SelectedAffixPreset);
                if (affixIndex != -1)
                {
                    affixIndex = (affixIndex + 1) % AffixPresets.Count;
                    SelectedAffixPreset = AffixPresets[affixIndex];
                }

                _eventAggregator.GetEvent<AffixPresetChangedEvent>().Publish(new AffixPresetChangedEventParams { PresetName = SelectedAffixPreset.Name });
            }
        }

        private void HandleSystemPresetMappingChangedEvent()
        {
            SetAffixCommand?.RaiseCanExecuteChanged();
            SetAspectCommand?.RaiseCanExecuteChanged();
            SetSigilCommand?.RaiseCanExecuteChanged();
        }

        private void HandleSystemPresetItemTypesLoadedEvent()
        {
            RaisePropertyChanged(nameof(IsItemTypeImageFound));
            RaisePropertyChanged(nameof(IsItemTypeImageHelmFound));
            RaisePropertyChanged(nameof(IsItemTypeImageChestFound));
            RaisePropertyChanged(nameof(IsItemTypeImageGlovesFound));
            RaisePropertyChanged(nameof(IsItemTypeImagePantsFound));
            RaisePropertyChanged(nameof(IsItemTypeImageBootsFound));
            RaisePropertyChanged(nameof(IsItemTypeImageAmuletFound));
            RaisePropertyChanged(nameof(IsItemTypeImageRingFound));
            RaisePropertyChanged(nameof(IsItemTypeImageWeaponFound));
            RaisePropertyChanged(nameof(IsItemTypeImageRangedFound));
            RaisePropertyChanged(nameof(IsItemTypeImageOffhandFound));
            RaisePropertyChanged(nameof(IsItemTypeImageConsumableFound));
            RaisePropertyChanged(nameof(IsItemTypeImageSigilFound));
            RaisePropertyChanged(nameof(IsItemTypeImageSeasonalFound));
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsAffixOverlayEnabled = toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayKeyBindingEvent()
        {
            IsAffixOverlayEnabled = !IsAffixOverlayEnabled;
        }

        private void RemoveAffixExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                _affixManager.RemoveAffix(itemAffix);
            }
        }

        private void RemoveAspectExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                _affixManager.RemoveAspect(itemAffix);
            }
        }

        private void RemoveSigilExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                _affixManager.RemoveSigil(itemAffix);
            }
        }

        private bool CanSetAffixExecute(AffixInfoVM affixInfo)
        {
            return _systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(affixInfo.IdName));
        }

        private async void SetAffixExecute(AffixInfoVM affixInfoVM)
        {
            if (affixInfoVM != null)
            {
                var setAffixDialog = new CustomDialog() { Title = "Set affix" };
                
                var dataContext = new SetAffixViewModel(async instance =>
                {
                    await setAffixDialog.WaitUntilUnloadedAsync();
                }, affixInfoVM.Model);
                setAffixDialog.Content = new SetAffixView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixDialog);
                await setAffixDialog.WaitUntilUnloadedAsync();
            }
        }

        private async void SetAffixColorExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
                var dataContext = new SetAffixColorViewModel(async instance =>
                {
                    await setAffixColorDialog.WaitUntilUnloadedAsync();
                }, itemAffix);
                setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }
        }

        private async void SetAffixMappingExecute(AffixInfoVM affixInfo)
        {
            if (affixInfo != null)
            {
                var setAffixMappingDialog = new CustomDialog() { Title = affixInfo.Description };
                var dataContext = new SetAffixMappingViewModel(async instance =>
                {
                    await setAffixMappingDialog.WaitUntilUnloadedAsync();
                }, affixInfo.Model);
                setAffixMappingDialog.Content = new SetAffixMappingView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixMappingDialog);
                await setAffixMappingDialog.WaitUntilUnloadedAsync();
            }
        }

        private bool CanSetAspectExecute(AspectInfoVM aspectInfo)
        {
            return _systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(aspectInfo.IdName));
        }

        private void SetAspectExecute(AspectInfoVM aspectInfo)
        {
            if (aspectInfo != null)
            {
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Helm);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Chest);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Gloves);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Pants);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Boots);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Amulet);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Ring);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Weapon);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Ranged);
                _affixManager.AddAspect(aspectInfo.Model, ItemTypeConstants.Offhand);
            }
        }

        private async void SetAspectColorExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
                var dataContext = new SetAffixColorViewModel(async instance =>
                {
                    await setAffixColorDialog.WaitUntilUnloadedAsync();
                }, itemAffix);
                setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
                await setAffixColorDialog.WaitUntilUnloadedAsync();

                // Set same color to all other gear slots
                foreach (var aspect in _selectedAspects) 
                {
                    if (aspect.Id.Equals(itemAffix.Id))
                    {
                        aspect.Color = itemAffix.Color;
                    }
                }
                _affixManager.SaveAffixColor(itemAffix);
            }
        }

        private async void SetAspectMappingExecute(AspectInfoVM aspectInfo)
        {
            if (aspectInfo != null)
            {
                var setAspectMappingDialog = new CustomDialog() { Title = aspectInfo.Name };
                var dataContext = new SetAspectMappingViewModel(async instance =>
                {
                    await setAspectMappingDialog.WaitUntilUnloadedAsync();
                }, aspectInfo.Model);
                setAspectMappingDialog.Content = new SetAspectMappingView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAspectMappingDialog);
                await setAspectMappingDialog.WaitUntilUnloadedAsync();
            }
        }

        private bool CanSetSigilExecute(SigilInfoVM sigilInfo)
        {
            return _systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(sigilInfo.IdName));
        }

        private void SetSigilExecute(SigilInfoVM sigilInfo)
        {
            if (sigilInfo != null)
            {
                _affixManager.AddSigil(sigilInfo.Model, ItemTypeConstants.Sigil);
            }
        }

        private async void SetSigilMappingExecute(SigilInfoVM sigilInfo)
        {
            if (sigilInfo != null)
            {
                var setSigilMappingDialog = new CustomDialog() { Title = sigilInfo.Name };
                var dataContext = new SetSigilMappingViewModel(async instance =>
                {
                    await setSigilMappingDialog.WaitUntilUnloadedAsync();
                }, sigilInfo.Model);
                setSigilMappingDialog.Content = new SetSigilMappingView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setSigilMappingDialog);
                await setSigilMappingDialog.WaitUntilUnloadedAsync();
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
            _affixManager.AddAffixPreset(new AffixPreset
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

            AffixInfoVM affixInfoVM = (AffixInfoVM)affixObj;

            if (!affixInfoVM.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (ToggleCore)
            {
                allowed = affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleBarbarian)
            {
                allowed = affixInfoVM.AllowedForPlayerClass[2] == 1 && !affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleDruid)
            {
                allowed = affixInfoVM.AllowedForPlayerClass[1] == 1 && !affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleNecromancer)
            {
                allowed = affixInfoVM.AllowedForPlayerClass[4] == 1 && !affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleRogue)
            {
                allowed = affixInfoVM.AllowedForPlayerClass[3] == 1 && !affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleSorcerer)
            {
                allowed = affixInfoVM.AllowedForPlayerClass[0] == 1 && !affixInfoVM.AllowedForPlayerClass.All(c => c == 1);
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

            AspectInfoVM aspectInfo = (AspectInfoVM)aspectObj;

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

        private void CreateItemSigilsFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SigilsFiltered = new ListCollectionView(Sigils)
                {
                    Filter = FilterSigils
                };
            });
        }

        private bool FilterSigils(object sigilObj)
        {
            var allowed = true;
            if (sigilObj == null) return false;

            SigilInfoVM sigilInfo = (SigilInfoVM)sigilObj;

            if (!sigilInfo.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !sigilInfo.Name.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (ToggleDungeons)
            {
                allowed = sigilInfo.Type.Equals(Constants.SigilTypeConstants.Dungeon);
            }
            else if (TogglePositive)
            {
                allowed = sigilInfo.Type.Equals(Constants.SigilTypeConstants.Positive);
            }
            else if (ToggleMajor)
            {
                allowed = sigilInfo.Type.Equals(Constants.SigilTypeConstants.Major);
            }
            else if (ToggleMinor)
            {
                allowed = sigilInfo.Type.Equals(Constants.SigilTypeConstants.Minor);
            }

            return allowed;
        }

        private void CreateSelectedAffixesHelmFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredHelm = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesHelm
                };
            });
        }

        private bool FilterSelectedAffixesHelm(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Helm);
        }

        private void CreateSelectedAffixesChestFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredChest = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesChest
                };
            });
        }

        private bool FilterSelectedAffixesChest(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Chest);
        }

        private void CreateSelectedAffixesGlovesFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredGloves = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesGloves
                };
            });
        }

        private bool FilterSelectedAffixesGloves(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Gloves);
        }

        private void CreateSelectedAffixesPantsFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredPants = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesPants
                };
            });
        }

        private bool FilterSelectedAffixesPants(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Pants);
        }

        private void CreateSelectedAffixesBootsFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredBoots = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesBoots
                };
            });
        }

        private bool FilterSelectedAffixesBoots(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Boots);
        }

        private void CreateSelectedAffixesAmuletFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredAmulet = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesAmulet
                };
            });
        }

        private bool FilterSelectedAffixesAmulet(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Amulet);
        }

        private void CreateSelectedAffixesRingFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredRing = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesRing
                };
            });
        }

        private bool FilterSelectedAffixesRing(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Ring);
        }

        private void CreateSelectedAffixesWeaponFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredWeapon = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesWeapon
                };
            });
        }

        private bool FilterSelectedAffixesWeapon(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Weapon);
        }

        private void CreateSelectedAffixesRangedFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredRanged = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesRanged
                };
            });
        }

        private bool FilterSelectedAffixesRanged(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Ranged);
        }

        private void CreateSelectedAffixesOffhandFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFilteredOffhand = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixesOffhand
                };
            });
        }

        private bool FilterSelectedAffixesOffhand(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAffixObj;

            return itemAffix.Type.Equals(ItemTypeConstants.Offhand);
        }

        private void CreateSelectedAspectsFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAspectsFiltered = new ListCollectionView(SelectedAspects)
                {
                    Filter = FilterSelectedAspects
                };
            });
        }

        private bool FilterSelectedAspects(object selectedAspectObj)
        {
            if (selectedAspectObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)selectedAspectObj;

            return !SelectedAspectsFiltered?.Cast<ItemAffix>().Any(a => a.Id.Equals(itemAffix.Id)) ?? false;
        }

        private void InitAffixlanguages()
        {
            _affixLanguages.Clear();
            _affixLanguages.Add(new AffixLanguage("deDE", "German"));
            _affixLanguages.Add(new AffixLanguage("enUS", "English"));
            _affixLanguages.Add(new AffixLanguage("esES", "Spanish (EU)"));
            _affixLanguages.Add(new AffixLanguage("esMX", "Spanish (LA)"));
            _affixLanguages.Add(new AffixLanguage("frFR", "French"));
            _affixLanguages.Add(new AffixLanguage("itIT", "Italian"));
            _affixLanguages.Add(new AffixLanguage("jaJP", "Japanese"));
            _affixLanguages.Add(new AffixLanguage("koKR", "Korean"));
            _affixLanguages.Add(new AffixLanguage("plPL", "Polish"));
            _affixLanguages.Add(new AffixLanguage("ptBR", "Portuguese"));
            _affixLanguages.Add(new AffixLanguage("ruRU", "Russian"));
            _affixLanguages.Add(new AffixLanguage("trTR", "Turkish"));
            _affixLanguages.Add(new AffixLanguage("zhCN", "Chinese (Simplified)"));
            _affixLanguages.Add(new AffixLanguage("zhTW", "Chinese (Traditional)"));

            var language = _affixLanguages.FirstOrDefault(language => language.Id.Equals(_settingsManager.Settings.SelectedAffixLanguage));
            if (language != null)
            {
                SelectedAffixLanguage = language;
            }
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
                    var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
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

        private void UpdateSelectedAspects()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedAspects.Clear();
                if (SelectedAffixPreset != null)
                {
                    SelectedAspects.AddRange(SelectedAffixPreset.ItemAspects);
                }
            });
        }

        private void UpdateSelectedSigils()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedSigils.Clear();
                if (SelectedAffixPreset != null)
                {
                    SelectedSigils.AddRange(SelectedAffixPreset.ItemSigils);
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
