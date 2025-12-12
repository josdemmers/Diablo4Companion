using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.ViewModels.Dialogs;
using D4Companion.ViewModels.Entities;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
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
        private readonly IBuildsManagerMaxroll _buildsManager;
        private readonly IBuildsManagerD4Builds _buildsManagerD4Builds;
        private readonly IBuildsManagerMobalytics _buildsManagerMobalytics;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<AffixInfoBase> _affixes = new ObservableCollection<AffixInfoBase>();
        private ObservableCollection<AffixLanguage> _affixLanguages = new ObservableCollection<AffixLanguage>();
        private ObservableCollection<AffixPreset> _affixPresets = new ObservableCollection<AffixPreset>();
        private ObservableCollection<AspectInfoBase> _aspects = new ObservableCollection<AspectInfoBase>();
        private ObservableCollection<BuildImportWebsite> _buildImportWebsites = new ObservableCollection<BuildImportWebsite>();
        private ObservableCollection<ItemAffix> _selectedAffixes = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedAspects = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedSigils = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedUniques = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _selectedRunes = new ObservableCollection<ItemAffix>();
        private ObservableCollection<SigilInfoBase> _sigils = new ObservableCollection<SigilInfoBase>();
        private ObservableCollection<UniqueInfoBase> _uniques = new ObservableCollection<UniqueInfoBase>();
        private ObservableCollection<RuneInfoBase> _runes = new ObservableCollection<RuneInfoBase>();

        private string _affixPresetName = string.Empty;
        private string _affixTextFilter = string.Empty;
        private int? _badgeCount = null;
        private bool _isAffixOverlayEnabled = false;
        private AffixLanguage _selectedAffixLanguage = new AffixLanguage();
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private BuildImportWebsite _selectedBuildImportWebsite = new BuildImportWebsite();
        private int _selectedTabIndex = 0;

        // Start of Constructors region

        #region Constructors

        public AffixViewModel(IEventAggregator eventAggregator, ILogger<AffixViewModel> logger, IAffixManager affixManager, IBuildsManagerMaxroll buildsManager, IBuildsManagerD4Builds buildsManagerD4Builds, IBuildsManagerMobalytics buildsManagerMobalytics,
            IDialogCoordinator dialogCoordinator, ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Subscribe(HandleSelectedAffixesChangedEvent);
            _eventAggregator.GetEvent<SelectedAspectsChangedEvent>().Subscribe(HandleSelectedAspectsChangedEvent);
            _eventAggregator.GetEvent<SelectedSigilsChangedEvent>().Subscribe(HandleSelectedSigilsChangedEvent);
            _eventAggregator.GetEvent<SelectedUniquesChangedEvent>().Subscribe(HandleSelectedUniquesChangedEvent);
            _eventAggregator.GetEvent<SelectedRunesChangedEvent>().Subscribe(HandleSelectedRunesChangedEvent);
            _eventAggregator.GetEvent<SwitchPresetKeyBindingEvent>().Subscribe(HandleSwitchPresetKeyBindingEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayKeyBindingEvent>().Subscribe(HandleToggleOverlayKeyBindingEvent);
            
            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _buildsManager = buildsManager;
            _buildsManagerD4Builds = buildsManagerD4Builds;
            _buildsManagerMobalytics = buildsManagerMobalytics;
            _dialogCoordinator = dialogCoordinator;
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init View commands
            AddAffixPresetNameCommand = new DelegateCommand(AddAffixPresetNameExecute, CanAddAffixPresetNameExecute);
            AffixConfigCommand = new DelegateCommand(AffixConfigExecute);
            AspectConfigCommand = new DelegateCommand(AspectConfigExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            ImportAffixPresetCommand = new DelegateCommand(ImportAffixPresetCommandExecute, CanImportAffixPresetCommandExecute);
            EditAffixCommand = new DelegateCommand<ItemAffix>(EditAffixExecute);
            RenameAffixPresetCommand = new DelegateCommand(RenameAffixPresetCommandExecute, CanRenameAffixPresetCommandExecute);
            RemoveAffixCommand = new DelegateCommand<ItemAffix>(RemoveAffixExecute);
            RemoveAspectCommand = new DelegateCommand<ItemAffix>(RemoveAspectExecute);
            RemoveSigilCommand = new DelegateCommand<ItemAffix>(RemoveSigilExecute);
            RemoveUniqueCommand = new DelegateCommand<ItemAffix>(RemoveUniqueExecute);
            RemoveRuneCommand = new DelegateCommand<ItemAffix>(RemoveRuneExecute);
            SetAffixCommand = new DelegateCommand<AffixInfoWanted>(SetAffixExecute);
            SetAffixColorCommand = new DelegateCommand<ItemAffix>(SetAffixColorExecute);
            SetAspectCommand = new DelegateCommand<AspectInfoWanted>(SetAspectExecute);
            SetAspectColorCommand = new DelegateCommand<ItemAffix>(SetAspectColorExecute);
            SetSigilCommand = new DelegateCommand<SigilInfoWanted>(SetSigilExecute);
            SetSigilDungeonTierToNextCommand = new DelegateCommand<SigilInfoWanted>(SetSigilDungeonTierToNextExecute);
            SetUniqueCommand = new DelegateCommand<UniqueInfoWanted>(SetUniqueExecute);
            SetRuneCommand = new DelegateCommand<RuneInfoWanted>(SetRuneExecute);
            SigilConfigCommand = new DelegateCommand(SigilConfigExecute);
            ToggleOverlayCommand = new DelegateCommand<bool?>(ToggleOverlayExecute);
            ToggleCoreCommand = new DelegateCommand<bool?>(ToggleCoreExecute);
            ToggleBarbarianCommand = new DelegateCommand<bool?>(ToggleBarbarianExecute);
            ToggleDruidCommand = new DelegateCommand<bool?>(ToggleDruidExecute);
            ToggleNecromancerCommand = new DelegateCommand<bool?>(ToggleNecromancerExecute);
            TogglePaladinCommand = new DelegateCommand<bool?>(TogglePaladinExecute);
            ToggleRogueCommand = new DelegateCommand<bool?>(ToggleRogueExecute);
            ToggleSorcererCommand = new DelegateCommand<bool?>(ToggleSorcererExecute);
            ToggleSpiritbornCommand = new DelegateCommand<bool?>(ToggleSpiritbornExecute);
            ToggleDungeonsCommand = new DelegateCommand<bool?>(ToggleDungeonsExecute);
            TogglePositiveCommand = new DelegateCommand<bool?>(TogglePositiveExecute);
            ToggleMinorCommand = new DelegateCommand<bool?>(ToggleMinorExecute);
            ToggleMajorCommand = new DelegateCommand<bool?>(ToggleMajorExecute);
            ToggleRuneConditionCommand = new DelegateCommand<bool?>(ToggleRuneConditionExecute);
            ToggleRuneEffectCommand = new DelegateCommand<bool?>(ToggleRuneEffectExecute);
            UniqueConfigCommand = new DelegateCommand(UniqueConfigExecute);
            RuneConfigCommand = new DelegateCommand(RuneConfigExecute);

            // Init filter views
            CreateItemAffixesFilteredView();
            CreateItemAspectsFilteredView();
            CreateItemSigilsFilteredView();
            CreateItemUniquesFilteredView();
            CreateItemRunesFilteredView();
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
            InitAffixLanguages();
            InitBuildImportWebsites();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfoBase> Affixes { get => _affixes; set => _affixes = value; }
        public ObservableCollection<AffixLanguage> AffixLanguages { get => _affixLanguages; set => _affixLanguages = value; }
        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<AspectInfoBase> Aspects { get => _aspects; set => _aspects = value; }
        public ObservableCollection<BuildImportWebsite> BuildImportWebsites { get => _buildImportWebsites; set => _buildImportWebsites = value; }
        public ObservableCollection<ItemAffix> SelectedAffixes { get => _selectedAffixes; set => _selectedAffixes = value; }
        public ObservableCollection<ItemAffix> SelectedAspects { get => _selectedAspects; set => _selectedAspects = value; }
        public ObservableCollection<ItemAffix> SelectedSigils { get => _selectedSigils; set => _selectedSigils = value; }
        public ObservableCollection<ItemAffix> SelectedUniques { get => _selectedUniques; set => _selectedUniques = value; }
        public ObservableCollection<ItemAffix> SelectedRunes { get => _selectedRunes; set => _selectedRunes = value; }
        public ObservableCollection<SigilInfoBase> Sigils { get => _sigils; set => _sigils = value; }
        public ObservableCollection<UniqueInfoBase> Uniques { get => _uniques; set => _uniques = value; }
        public ObservableCollection<RuneInfoBase> Runes { get => _runes; set => _runes = value; }
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
        public ListCollectionView? UniquesFiltered { get; private set; }
        public ListCollectionView? RunesFiltered { get; private set; }

        public DelegateCommand AddAffixPresetNameCommand { get; }
        public DelegateCommand AffixConfigCommand { get; }
        public DelegateCommand AspectConfigCommand { get; }
        public DelegateCommand<ItemAffix> EditAffixCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand ImportAffixPresetCommand { get; }
        public DelegateCommand RenameAffixPresetCommand { get; }
        public DelegateCommand<ItemAffix> RemoveAffixCommand { get; }
        public DelegateCommand<ItemAffix> RemoveAspectCommand { get; }
        public DelegateCommand<ItemAffix> RemoveSigilCommand { get; }
        public DelegateCommand<ItemAffix> RemoveUniqueCommand { get; }
        public DelegateCommand<ItemAffix> RemoveRuneCommand { get; }
        public DelegateCommand<AffixInfoWanted> SetAffixCommand { get; }
        public DelegateCommand<ItemAffix> SetAffixColorCommand { get; }
        public DelegateCommand<AspectInfoWanted> SetAspectCommand { get; }
        public DelegateCommand<ItemAffix> SetAspectColorCommand { get; }
        public DelegateCommand<SigilInfoWanted> SetSigilCommand { get; }
        public DelegateCommand<SigilInfoWanted> SetSigilDungeonTierToNextCommand { get; }
        public DelegateCommand<UniqueInfoWanted> SetUniqueCommand { get; }
        public DelegateCommand<RuneInfoWanted> SetRuneCommand { get; }
        public DelegateCommand SigilConfigCommand { get; }
        public DelegateCommand<bool?> ToggleOverlayCommand { get; }
        public DelegateCommand<bool?> ToggleCoreCommand { get; }
        public DelegateCommand<bool?> ToggleBarbarianCommand { get; }
        public DelegateCommand<bool?> ToggleDruidCommand { get; }
        public DelegateCommand<bool?> ToggleNecromancerCommand { get; }
        public DelegateCommand<bool?> TogglePaladinCommand { get; }
        public DelegateCommand<bool?> ToggleRogueCommand { get; }
        public DelegateCommand<bool?> ToggleSorcererCommand { get; }
        public DelegateCommand<bool?> ToggleSpiritbornCommand { get; }
        public DelegateCommand<bool?> ToggleDungeonsCommand { get; }
        public DelegateCommand<bool?> TogglePositiveCommand { get; }
        public DelegateCommand<bool?> ToggleMinorCommand { get; }
        public DelegateCommand<bool?> ToggleMajorCommand { get; }
        public DelegateCommand<bool?> ToggleRuneConditionCommand { get; }
        public DelegateCommand<bool?> ToggleRuneEffectCommand { get; }
        public DelegateCommand UniqueConfigCommand { get; }
        public DelegateCommand RuneConfigCommand { get; }

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
                RefreshAffixViewFilter(); 
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

        public bool IsAffixesTabActive
        {
            get => SelectedTabIndex == 0;
        }

        public bool IsAspectsTabActive
        {
            get => SelectedTabIndex == 1;
        }

        public bool IsClassTabActive
        {
            get => IsAffixesTabActive || IsAspectsTabActive || IsUniquesTabActive;
        }

        public bool IsSigilsTabActive
        {
            get => SelectedTabIndex == 2;
        }

        public bool IsUniquesTabActive
        {
            get => SelectedTabIndex == 3;
        }

        public bool IsRunesTabActive
        {
            get => SelectedTabIndex == 4;
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
                    Affixes.Add(new AffixInfoConfig());
                    Affixes.AddRange(_affixManager.Affixes.Select(affixInfo => new AffixInfoWanted(affixInfo)));

                    Aspects.Clear();
                    Aspects.Add(new AspectInfoConfig());
                    Aspects.AddRange(_affixManager.Aspects.Select(aspectInfo => new AspectInfoWanted(aspectInfo)));

                    Sigils.Clear();
                    Sigils.Add(new SigilInfoConfig());
                    Sigils.AddRange(_affixManager.Sigils.Select(sigilInfo => new SigilInfoWanted(sigilInfo)));

                    Uniques.Clear();
                    Uniques.Add(new UniqueInfoConfig());
                    Uniques.AddRange(_affixManager.Uniques.Select(uniqueInfo => new UniqueInfoWanted(uniqueInfo)));

                    Runes.Clear();
                    Runes.Add(new RuneInfoConfig());
                    Runes.AddRange(_affixManager.Runes.Select(runeInfo => new RuneInfoWanted(runeInfo)));

                    UpdateSelectedAffixes();
                    UpdateSelectedAspects();
                    UpdateSelectedSigils();
                    UpdateSelectedUniques();
                    UpdateSelectedRunes();
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
                RenameAffixPresetCommand?.RaiseCanExecuteChanged();
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
                UpdateSelectedUniques();
                UpdateSelectedRunes();
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
                RaisePropertyChanged(nameof(IsClassTabActive));
                RaisePropertyChanged(nameof(IsSigilsTabActive));
                RaisePropertyChanged(nameof(IsUniquesTabActive));
                RaisePropertyChanged(nameof(IsRunesTabActive));

                RefreshAffixViewFilter();
            }
        }

        public BuildImportWebsite SelectedBuildImportWebsite
        {
            get => _selectedBuildImportWebsite;
            set
            {
                _selectedBuildImportWebsite = value;
                RaisePropertyChanged();
            }
        }

        public bool ToggleCore
        {
            get => _settingsManager.Settings.IsToggleCoreActive;
            set
            {
                _settingsManager.Settings.IsToggleCoreActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleCore));
                _settingsManager.SaveSettings();
            }
        }

        private void RefreshAffixViewFilter()
        {
            if (IsAffixesTabActive)
            {
                AffixesFiltered?.Refresh();
            }
            else if (IsAspectsTabActive)
            {
                AspectsFiltered?.Refresh();
            }
            else if (IsSigilsTabActive)
            {
                SigilsFiltered?.Refresh();
            }
            else if (IsUniquesTabActive)
            {
                UniquesFiltered?.Refresh();
            }
            else if (IsRunesTabActive)
            {
                RunesFiltered?.Refresh();
            }
        }

        public bool ToggleBarbarian
        {
            get => _settingsManager.Settings.IsToggleBarbarianActive;
            set
            {
                _settingsManager.Settings.IsToggleBarbarianActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleBarbarian));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleDruid
        {
            get => _settingsManager.Settings.IsToggleDruidActive;
            set
            {
                _settingsManager.Settings.IsToggleDruidActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleDruid));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleNecromancer
        {
            get => _settingsManager.Settings.IsToggleNecromancerActive;
            set
            {
                _settingsManager.Settings.IsToggleNecromancerActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleNecromancer));
                _settingsManager.SaveSettings();
            }
        }

        public bool TogglePaladin
        {
            get => _settingsManager.Settings.IsTogglePaladinActive;
            set
            {
                _settingsManager.Settings.IsTogglePaladinActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(TogglePaladin));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleRogue
        {
            get => _settingsManager.Settings.IsToggleRogueActive;
            set
            {
                _settingsManager.Settings.IsToggleRogueActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleRogue));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleSorcerer
        {
            get => _settingsManager.Settings.IsToggleSorcererActive;
            set
            {
                _settingsManager.Settings.IsToggleSorcererActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleSorcerer));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleSpiritborn
        {
            get => _settingsManager.Settings.IsToggleSpiritbornActive;
            set
            {
                _settingsManager.Settings.IsToggleSpiritbornActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged(nameof(ToggleSpiritborn));
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleDungeons
        {
            get => _settingsManager.Settings.IsToggleDungeonsActive;
            set
            {
                _settingsManager.Settings.IsToggleDungeonsActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        public bool TogglePositive
        {
            get => _settingsManager.Settings.IsTogglePositiveActive;
            set
            {
                _settingsManager.Settings.IsTogglePositiveActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleMinor
        {
            get => _settingsManager.Settings.IsToggleMinorActive;
            set
            {
                _settingsManager.Settings.IsToggleMinorActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleMajor
        {
            get => _settingsManager.Settings.IsToggleMajorActive;
            set
            {
                _settingsManager.Settings.IsToggleMajorActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleRuneCondition
        {
            get => _settingsManager.Settings.IsToggleRuneConditionActive;
            set
            {
                _settingsManager.Settings.IsToggleRuneConditionActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        public bool ToggleRuneEffect
        {
            get => _settingsManager.Settings.IsToggleRuneEffectActive;
            set
            {
                _settingsManager.Settings.IsToggleRuneEffectActive = value;
                RefreshAffixViewFilter();
                RaisePropertyChanged();
                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private async void EditAffixExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                var affixInfoVM = _affixes.OfType<AffixInfoWanted>().FirstOrDefault(a => a.IdName.Equals(itemAffix.Id));
                if (affixInfoVM == null) return;

                var setAffixDialog = new CustomDialog() { Title = "Set affix" };
                setAffixDialog.DialogContentWidth = GridLength.Auto;

                var dataContext = new SetAffixViewModel(async instance =>
                {
                    await setAffixDialog.WaitUntilUnloadedAsync();
                }, SelectedAffixPreset, affixInfoVM.Model);
                setAffixDialog.Content = new SetAffixView() { DataContext = dataContext };
                await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixDialog);
                await setAffixDialog.WaitUntilUnloadedAsync();
            }
        }

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
                Affixes.Add(new AffixInfoConfig());
                Affixes.AddRange(_affixManager.Affixes.Select(affixInfo => new AffixInfoWanted(affixInfo)));

                Aspects.Clear();
                Aspects.Add(new AspectInfoConfig());
                Aspects.AddRange(_affixManager.Aspects.Select(aspectInfo => new AspectInfoWanted(aspectInfo)));

                Sigils.Clear();
                Sigils.Add(new SigilInfoConfig());
                Sigils.AddRange(_affixManager.Sigils.Select(sigilInfo => new SigilInfoWanted(sigilInfo)));

                Uniques.Clear();
                Uniques.Add(new UniqueInfoConfig());
                Uniques.AddRange(_affixManager.Uniques.Select(uniqueInfo => new UniqueInfoWanted(uniqueInfo)));

                Runes.Clear();
                Runes.Add(new RuneInfoConfig());
                Runes.AddRange(_affixManager.Runes.Select(runeInfo => new RuneInfoWanted(runeInfo)));
            });

            // Load affix presets
            UpdateAffixPresets();

            // Load selected affixes
            UpdateSelectedAffixes();

            // Load selected aspects
            UpdateSelectedAspects();

            // Load selected sigils
            UpdateSelectedSigils();

            // Load selected uniques
            UpdateSelectedUniques();

            // Load selected runes
            UpdateSelectedRunes();
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

        private void HandleSelectedUniquesChangedEvent()
        {
            UpdateSelectedUniques();
        }

        private void HandleSelectedRunesChangedEvent()
        {
            UpdateSelectedRunes();
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

        private void RemoveUniqueExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                _affixManager.RemoveUnique(itemAffix);
            }
        }

        private void RemoveRuneExecute(ItemAffix itemAffix)
        {
            if (itemAffix != null)
            {
                _affixManager.RemoveRune(itemAffix);
            }
        }

        private async void SetAffixExecute(AffixInfoWanted affixInfo)
        {
            if (affixInfo != null)
            {
                var setAffixDialog = new CustomDialog() { Title = "Set affix" };
                setAffixDialog.DialogContentWidth = GridLength.Auto;

                var dataContext = new SetAffixViewModel(async instance =>
                {
                    await setAffixDialog.WaitUntilUnloadedAsync();
                }, SelectedAffixPreset, affixInfo.Model);
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

        private void SetAspectExecute(AspectInfoWanted aspectInfo)
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

        private void SetSigilExecute(SigilInfoWanted sigilInfo)
        {
            if (sigilInfo != null)
            {
                _affixManager.AddSigil(sigilInfo.Model, ItemTypeConstants.Sigil);
            }
        }

        private void SetSigilDungeonTierToNextExecute(SigilInfoWanted sigilInfo)
        {
            if (sigilInfo != null)
            {
                int tierIndex = sigilInfo.Tiers.IndexOf(sigilInfo.Tier);
                int nextTierIndex = (tierIndex + 1) % sigilInfo.Tiers.Count;
                string nextTier = sigilInfo.Tiers[nextTierIndex];
                _affixManager.SetSigilDungeonTier(sigilInfo.Model, nextTier);
            }
        }

        private void SetUniqueExecute(UniqueInfoWanted uniqueInfo)
        {
            if (uniqueInfo != null)
            {
                _affixManager.AddUnique(uniqueInfo.Model);
            }
        }

        private void SetRuneExecute(RuneInfoWanted runeInfo)
        {
            if (runeInfo != null)
            {
                _affixManager.AddRune(runeInfo.Model);
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

        private async void AffixConfigExecute()
        {
            var affixConfigDialog = new CustomDialog() { Title = "Affix config" };
            var dataContext = new AffixConfigViewModel(async instance =>
            {
                await affixConfigDialog.WaitUntilUnloadedAsync();
            });
            affixConfigDialog.Content = new AffixConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, affixConfigDialog);
            await affixConfigDialog.WaitUntilUnloadedAsync();
        }

        private async void AspectConfigExecute()
        {
            var aspectConfigDialog = new CustomDialog() { Title = "Aspect config" };
            var dataContext = new AspectConfigViewModel(async instance =>
            {
                await aspectConfigDialog.WaitUntilUnloadedAsync();
            });
            aspectConfigDialog.Content = new AspectConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, aspectConfigDialog);
            await aspectConfigDialog.WaitUntilUnloadedAsync();
        }

        private async void SigilConfigExecute()
        {
            var sigilConfigDialog = new CustomDialog() { Title = "Sigil config" };
            var dataContext = new SigilConfigViewModel(async instance =>
            {
                await sigilConfigDialog.WaitUntilUnloadedAsync();
            });
            sigilConfigDialog.Content = new SigilConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, sigilConfigDialog);
            await sigilConfigDialog.WaitUntilUnloadedAsync();
        }

        private void ToggleOverlayExecute(bool? isEnabled)
        {
            IsAffixOverlayEnabled = isEnabled ?? false;
        }

        private void ToggleCoreExecute(bool? isActive)
        {
            ToggleCore = isActive ?? false;
        }

        private void ToggleBarbarianExecute(bool? isActive)
        {
            ToggleBarbarian = isActive ?? false;
        }

        private void ToggleDruidExecute(bool? isActive)
        {
            ToggleDruid = isActive ?? false;
        }

        private void ToggleNecromancerExecute(bool? isActive)
        {
            ToggleNecromancer = isActive ?? false;
        }

        private void TogglePaladinExecute(bool? isActive)
        {
            TogglePaladin = isActive ?? false;
        }

        private void ToggleRogueExecute(bool? isActive)
        {
            ToggleRogue = isActive ?? false;
        }

        private void ToggleSorcererExecute(bool? isActive)
        {
            ToggleSorcerer = isActive ?? false;
        }

        private void ToggleSpiritbornExecute(bool? isActive)
        {
            ToggleSpiritborn = isActive ?? false;
        }

        private void ToggleDungeonsExecute(bool? isActive)
        {
            ToggleDungeons = isActive ?? false;
        }

        private void TogglePositiveExecute(bool? isActive)
        {
            TogglePositive = isActive ?? false;
        }

        private void ToggleMinorExecute(bool? isActive)
        {
            ToggleMinor = isActive ?? false;
        }

        private void ToggleMajorExecute(bool? isActive)
        {
            ToggleMajor = isActive ?? false;
        }

        private void ToggleRuneConditionExecute(bool? isActive)
        {
            ToggleRuneCondition = isActive ?? false;
        }

        private void ToggleRuneEffectExecute(bool? isActive)
        {
            ToggleRuneEffect = isActive ?? false;
        }

        private async void UniqueConfigExecute()
        {
            var uniqueConfigDialog = new CustomDialog() { Title = "Unique config" };
            var dataContext = new UniqueConfigViewModel(async instance =>
            {
                await uniqueConfigDialog.WaitUntilUnloadedAsync();
            });
            uniqueConfigDialog.Content = new UniqueConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, uniqueConfigDialog);
            await uniqueConfigDialog.WaitUntilUnloadedAsync();
        }

        private async void RuneConfigExecute()
        {
            var runeConfigDialog = new CustomDialog() { Title = "Rune config" };
            var dataContext = new RuneConfigViewModel(async instance =>
            {
                await runeConfigDialog.WaitUntilUnloadedAsync();
            });
            runeConfigDialog.Content = new RuneConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, runeConfigDialog);
            await runeConfigDialog.WaitUntilUnloadedAsync();
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

                AffixesFiltered.CustomSort = new AffixInfoCustomSort();
            });
        }

        private bool FilterAffixes(object affixObj)
        {
            if (affixObj == null) return false;
            if (affixObj.GetType() == typeof(AffixInfoConfig)) return true;

            AffixInfoWanted affixInfo = (AffixInfoWanted)affixObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!affixInfo.Description.ToLower().Contains(keyword.Trim().ToLower()))
                {
                    return false;
                }
            }

            if (ToggleCore && (affixInfo.AllowedForPlayerClass.All(c => c == 1) || affixInfo.AllowedForPlayerClass.All(c => c == 0)))
            {
                return true;
            }

            if (ToggleBarbarian && affixInfo.AllowedForPlayerClass[2] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }

            if (ToggleDruid && affixInfo.AllowedForPlayerClass[1] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }

            if (ToggleNecromancer && affixInfo.AllowedForPlayerClass[4] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }

            // TODO: Need paladin index
            //if (TogglePaladin && affixInfo.AllowedForPlayerClass[6] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            //{
            //    return true;
            //}

            if (ToggleRogue && affixInfo.AllowedForPlayerClass[3] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }
            
            if (ToggleSorcerer && affixInfo.AllowedForPlayerClass[0] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }

            if (ToggleSpiritborn && affixInfo.AllowedForPlayerClass[5] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                return true;
            }

            return false;
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

                AspectsFiltered.CustomSort = new AspectInfoCustomSort();
            });
        }

        private bool FilterAspects(object aspectObj)
        {
            var allowed = true;
            if (aspectObj == null) return false;
            if (aspectObj.GetType() == typeof(AspectInfoConfig)) return true;

            AspectInfoWanted aspectInfo = (AspectInfoWanted)aspectObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!aspectInfo.Description.ToLower().Contains(keyword.Trim().ToLower()) && !aspectInfo.Name.ToLower().Contains(keyword.Trim().ToLower()) && !string.IsNullOrWhiteSpace(keyword))
                {
                    return false;
                }
            }

            if (aspectInfo.AllowedForPlayerClass.All(c => c == 1) ||
                aspectInfo.AllowedForPlayerClass.All(c => c == 0))
            {
                allowed = ToggleCore;
                if (allowed) return allowed;
            }

            if (aspectInfo.AllowedForPlayerClass[2] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleBarbarian;
            }
            else if (aspectInfo.AllowedForPlayerClass[1] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleDruid;
            }
            else if (aspectInfo.AllowedForPlayerClass[4] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleNecromancer;
            }
            else if (aspectInfo.AllowedForPlayerClass[3] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleRogue;
            }
            else if (aspectInfo.AllowedForPlayerClass[0] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleSorcerer;
            }
            else if (aspectInfo.AllowedForPlayerClass[5] == 1 && !aspectInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleSpiritborn;
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

                SigilsFiltered.CustomSort = new SigilInfoCustomSort();
            });
        }

        private bool FilterSigils(object sigilObj)
        {
            var allowed = true;
            if (sigilObj == null) return false;
            if (sigilObj.GetType() == typeof(SigilInfoConfig)) return true;

            SigilInfoWanted sigilInfo = (SigilInfoWanted)sigilObj;

            if (!sigilInfo.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !sigilInfo.Name.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (sigilInfo.Type.Equals(Constants.SigilTypeConstants.Dungeon))
            {
                allowed = ToggleDungeons;
            }
            else if (sigilInfo.Type.Equals(Constants.SigilTypeConstants.Positive))
            {
                allowed = TogglePositive;
            }
            else if (sigilInfo.Type.Equals(Constants.SigilTypeConstants.Major))
            {
                allowed = ToggleMajor;
            }
            else if (sigilInfo.Type.Equals(Constants.SigilTypeConstants.Minor))
            {
                allowed = ToggleMinor;
            }
            else
            {
                allowed = false;
            }

            return allowed;
        }

        private void CreateItemUniquesFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                UniquesFiltered = new ListCollectionView(Uniques)
                {
                    Filter = FilterUniques
                };

                UniquesFiltered.CustomSort = new UniqueInfoCustomSort();
            });
        }

        private bool FilterUniques(object uniqueObj)
        {
            var allowed = true;
            if (uniqueObj == null) return false;
            if (uniqueObj.GetType() == typeof(UniqueInfoConfig)) return true;

            UniqueInfoWanted uniqueInfo = (UniqueInfoWanted)uniqueObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!uniqueInfo.Description.ToLower().Contains(keyword.Trim().ToLower()) && !uniqueInfo.Name.ToLower().Contains(keyword.Trim().ToLower()) && !string.IsNullOrWhiteSpace(keyword))
                {
                    return false;
                }
            }

            if (uniqueInfo.AllowedForPlayerClass.All(c => c == 1) ||
                uniqueInfo.AllowedForPlayerClass.All(c => c == 0))
            {
                allowed = ToggleCore;
                if (allowed) return allowed;
            }

            if (uniqueInfo.AllowedForPlayerClass[2] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleBarbarian;
            }
            else if (uniqueInfo.AllowedForPlayerClass[1] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleDruid;
            }
            else if (uniqueInfo.AllowedForPlayerClass[4] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleNecromancer;
            }
            else if (uniqueInfo.AllowedForPlayerClass[3] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleRogue;
            }
            else if (uniqueInfo.AllowedForPlayerClass[0] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleSorcerer;
            }
            else if (uniqueInfo.AllowedForPlayerClass[5] == 1 && !uniqueInfo.AllowedForPlayerClass.All(c => c == 1))
            {
                allowed = ToggleSpiritborn;
            }

            return allowed;
        }

        private void CreateItemRunesFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                RunesFiltered = new ListCollectionView(Runes)
                {
                    Filter = FilterRunes
                };

                RunesFiltered.CustomSort = new RuneInfoCustomSort();
            });
        }

        private bool FilterRunes(object runeObj)
        {
            var allowed = true;
            if (runeObj == null) return false;
            if (runeObj.GetType() == typeof(RuneInfoConfig)) return true;

            RuneInfoWanted runeInfo = (RuneInfoWanted)runeObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!runeInfo.Description.ToLower().Contains(keyword.Trim().ToLower()) && !runeInfo.Name.ToLower().Contains(keyword.Trim().ToLower()) && !string.IsNullOrWhiteSpace(keyword))
                {
                    return false;
                }
            }

            if (runeInfo.IsCondition)
            {
                allowed = ToggleRuneCondition;
            }
            else if (runeInfo.IsEffect)
            {
                allowed = ToggleRuneEffect;
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

        private void InitAffixLanguages()
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
        private void InitBuildImportWebsites()
        {
            BuildImportWebsites.Clear();
            BuildImportWebsites.Add(new BuildImportWebsite() { Name = "D4Builds.gg", Image = "/Images/website_icon_d4builds.png" });
            BuildImportWebsites.Add(new BuildImportWebsite() { Name = "Maxroll.gg", Image = "/Images/website_icon_maxroll.png" });
            BuildImportWebsites.Add(new BuildImportWebsite() { Name = "Mobalytics.gg", Image = "/Images/website_icon_mobalytics.png" });

            SelectedBuildImportWebsite = BuildImportWebsites[0];
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

        private void UpdateSelectedUniques()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedUniques.Clear();
                if (SelectedAffixPreset != null)
                {
                    SelectedUniques.AddRange(SelectedAffixPreset.ItemUniques);
                }
            });
        }

        private void UpdateSelectedRunes()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedRunes.Clear();
                if (SelectedAffixPreset != null)
                {
                    SelectedRunes.AddRange(SelectedAffixPreset.ItemRunes);
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

        private bool CanImportAffixPresetCommandExecute()
        {
            return true;
        }

        private async void ImportAffixPresetCommandExecute()
        {
            var importAffixPresetDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapImportPreset"] };
            var dataContext = new ImportAffixPresetViewModel(async instance =>
            {
                await importAffixPresetDialog.WaitUntilUnloadedAsync();
            }, _affixManager, _buildsManager, _buildsManagerD4Builds, _buildsManagerMobalytics, _settingsManager, _selectedBuildImportWebsite);
            importAffixPresetDialog.Content = new ImportAffixPresetView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, importAffixPresetDialog);
            await importAffixPresetDialog.WaitUntilUnloadedAsync();
        }

        private bool CanRenameAffixPresetCommandExecute()
        {
            return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
        }

        private async void RenameAffixPresetCommandExecute()
        {
            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedAffixPreset.Name
            };

            var renamePresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapRenamePreset"] };
            var dataContext = new RenamePresetNameViewModel(async instance =>
            {
                await renamePresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName);
            renamePresetNameDialog.Content = new RenamePresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, renamePresetNameDialog);
            await renamePresetNameDialog.WaitUntilUnloadedAsync();

            string oldName = SelectedAffixPreset.Name;
            string newName = presetName.String;

            // Apply confirmed preset name.
            if (!dataContext.IsCanceled)
            {
                _affixManager.RenamePreset(SelectedAffixPreset.Name, presetName.String);
                UpdateAffixPresets();
            }
        }

        #endregion
    }
}
