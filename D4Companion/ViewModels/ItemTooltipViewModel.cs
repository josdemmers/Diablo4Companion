using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace D4Companion.ViewModels
{
    public class ItemTooltipViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;
        private readonly IAffixPresetManager _affixPresetManager;

        private ObservableCollection<AffixPreset> _affixPresets = new ObservableCollection<AffixPreset>();
        private ObservableCollection<ItemAffix> _itemAffixes = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAffix> _itemAffixesActive = new ObservableCollection<ItemAffix>();
        private ObservableCollection<ItemAspect> _itemAspects = new ObservableCollection<ItemAspect>();
        private ObservableCollection<ItemAspect> _itemAspectsActive = new ObservableCollection<ItemAspect>();
        private ObservableCollection<ItemType> _itemTypes = new ObservableCollection<ItemType>();

        private string _affixPresetName = string.Empty;
        private string _affixNameFilter = string.Empty;
        private string _aspectNameFilter = string.Empty;
        private int? _badgeCount = null;
        private BitmapSource? _imageHead = null;
        private BitmapSource? _imageTorso = null;
        private BitmapSource? _imageHands = null;
        private BitmapSource? _imageLegs = null;
        private BitmapSource? _imageFeet = null;
        private BitmapSource? _imageNeck = null;
        private BitmapSource? _imageRing = null;
        private BitmapSource? _imageMainHand = null;
        private BitmapSource? _imageRanged = null;
        private BitmapSource? _imageOffHand = null;
        private BitmapSource? _imageSigil = null;
        private bool _isAffixOverlayEnabled = false;
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private bool _toggleHead = true;
        private bool _toggleTorso = false;
        private bool _toggleHands = false;
        private bool _toggleLegs = false;
        private bool _toggleFeet = false;
        private bool _toggleNeck = false;
        private bool _toggleRing = false;
        private bool _toggleMainHand = false;
        private bool _toggleRanged = false;
        private bool _toggleOffHand = false;
        private bool _toggleSigil = false;

        // Start of Constructors region

        #region Constructors

        public ItemTooltipViewModel(IEventAggregator eventAggregator, ILogger<ItemTooltipViewModel> logger, ISettingsManager settingsManager, IAffixPresetManager affixPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<ReloadAffixesGuiRequestEvent>().Subscribe(HandleReloadAffixesGuiRequestEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);            

            // Init logger
            _logger = logger;

            // Init services
            _affixPresetManager = affixPresetManager;
            _settingsManager = settingsManager;

            // Init View commands
            ApplicationLoadedCommand = new DelegateCommand(ApplicationLoadedExecute);
            AddAffixPresetNameCommand = new DelegateCommand(AddAffixPresetNameExecute, CanAddAffixPresetNameExecute);
            ActiveAffixDoubleClickedCommand = new DelegateCommand<object>(ActiveAffixDoubleClickedExecute);
            InactiveAffixDoubleClickedCommand = new DelegateCommand<object>(InactiveAffixDoubleClickedExecute);
            ActiveAspectDoubleClickedCommand = new DelegateCommand<object>(ActiveAspectDoubleClickedExecute);
            InactiveAspectDoubleClickedCommand = new DelegateCommand<object>(InactiveAspectDoubleClickedExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);

            // Init filter views
            CreateItemAffixesFilteredView();
            CreateItemAffixesActiveFilteredView();
            CreateItemAspectsFilteredView();
            CreateItemAspectsActiveFilteredView();
            CreateItemTypeFilteredView();

            // Load item type icons
            LoadItemTypeIcons();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand AddAffixPresetNameCommand { get; }
        public DelegateCommand ApplicationLoadedCommand { get; }
        public DelegateCommand<object> ActiveAffixDoubleClickedCommand { get; }
        public DelegateCommand<object> InactiveAffixDoubleClickedCommand { get; }
        public DelegateCommand<object> ActiveAspectDoubleClickedCommand { get; }
        public DelegateCommand<object> InactiveAspectDoubleClickedCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<ItemAffix> ItemAffixes { get => _itemAffixes; set => _itemAffixes = value; }
        public ObservableCollection<ItemAffix> ItemAffixesActive { get => _itemAffixesActive; set => _itemAffixesActive = value; }
        public ObservableCollection<ItemAspect> ItemAspects { get => _itemAspects; set => _itemAspects = value; }
        public ObservableCollection<ItemAspect> ItemAspectsActive { get => _itemAspectsActive; set => _itemAspectsActive = value; }
        public ObservableCollection<ItemType> ItemTypes { get => _itemTypes; set => _itemTypes = value; }
        public ListCollectionView? ItemAffixesFiltered { get; private set; }
        public ListCollectionView? ItemAffixesActiveFiltered { get; private set; }
        public ListCollectionView? ItemAspectsFiltered { get; private set; }
        public ListCollectionView? ItemAspectsActiveFiltered { get; private set; }
        public ListCollectionView? ItemTypesFiltered { get; private set; }

        public string AffixPresetName
        {
            get => _affixPresetName;
            set
            {
                SetProperty(ref _affixPresetName, value, () => { RaisePropertyChanged(nameof(AffixPresetName)); });
                AddAffixPresetNameCommand?.RaiseCanExecuteChanged();
            }
        }

        public string AffixNameFilter
        {
            get => _affixNameFilter;
            set
            {
                SetProperty(ref _affixNameFilter, value, () => { RaisePropertyChanged(nameof(AffixNameFilter)); });
                ItemAffixesFiltered?.Refresh();
            }
        }

        public string AspectNameFilter
        {
            get => _aspectNameFilter;
            set
            {
                SetProperty(ref _aspectNameFilter, value, () => { RaisePropertyChanged(nameof(AspectNameFilter)); });
                ItemAspectsFiltered?.Refresh();
            }
        }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public BitmapSource? ImageHead { get => _imageHead; set => SetProperty(ref _imageHead, value, () => { RaisePropertyChanged(nameof(ImageHead)); }); }
        public BitmapSource? ImageTorso { get => _imageTorso; set => SetProperty(ref _imageTorso, value, () => { RaisePropertyChanged(nameof(ImageTorso)); }); }
        public BitmapSource? ImageHands { get => _imageHands; set => SetProperty(ref _imageHands, value, () => { RaisePropertyChanged(nameof(ImageHands)); }); }
        public BitmapSource? ImageLegs { get => _imageLegs; set => SetProperty(ref _imageLegs, value, () => { RaisePropertyChanged(nameof(ImageLegs)); }); }
        public BitmapSource? ImageFeet { get => _imageFeet; set => SetProperty(ref _imageFeet, value, () => { RaisePropertyChanged(nameof(ImageFeet)); }); }
        public BitmapSource? ImageNeck { get => _imageNeck; set => SetProperty(ref _imageNeck, value, () => { RaisePropertyChanged(nameof(ImageNeck)); }); }
        public BitmapSource? ImageRing { get => _imageRing; set => SetProperty(ref _imageRing, value, () => { RaisePropertyChanged(nameof(ImageRing)); }); }
        public BitmapSource? ImageMainHand { get => _imageMainHand; set => SetProperty(ref _imageMainHand, value, () => { RaisePropertyChanged(nameof(ImageMainHand)); }); }
        public BitmapSource? ImageRanged { get => _imageRanged; set => SetProperty(ref _imageRanged, value, () => { RaisePropertyChanged(nameof(ImageRanged)); }); }
        public BitmapSource? ImageOffHand { get => _imageOffHand; set => SetProperty(ref _imageOffHand, value, () => { RaisePropertyChanged(nameof(ImageOffHand)); }); }
        public BitmapSource? ImageSigil { get => _imageSigil; set => SetProperty(ref _imageSigil, value, () => { RaisePropertyChanged(nameof(ImageSigil)); }); }

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
                    _settingsManager.Settings.SelectedAffixName = value.Name;
                    _settingsManager.SaveSettings();
                    UpdateItemAffixesActive();
                }
                else
                {
                    _selectedAffixPreset = new AffixPreset();
                }
            }
        }

        public string SelectedSystemPreset => _settingsManager.Settings.SelectedSystemPreset;

        public bool ToggleHead
        {
            get => _toggleHead;
            set
            {
                _toggleHead = value;

                if (value)
                {
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleTorso
        {
            get => _toggleTorso;
            set
            {
                _toggleTorso = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleHands
        {
            get => _toggleHands;
            set
            {
                _toggleHands = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleLegs
        {
            get => _toggleLegs;
            set
            {
                _toggleLegs = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleFeet
        {
            get => _toggleFeet;
            set
            {
                _toggleFeet = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleNeck
        {
            get => _toggleNeck;
            set
            {
                _toggleNeck = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleRing
        {
            get => _toggleRing;
            set
            {
                _toggleRing = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleMainHand
        {
            get => _toggleMainHand;
            set
            {
                _toggleMainHand = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleRanged
        {
            get => _toggleRanged;
            set
            {
                _toggleRanged = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleOffHand = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleOffHand
        {
            get => _toggleOffHand;
            set
            {
                _toggleOffHand = value;

                if (value)
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleSigil = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }

        public bool ToggleSigil
        {
            get => _toggleSigil;
            set
            {
                _toggleSigil = value;

                if (value) 
                {
                    ToggleHead = false;
                    ToggleTorso = false;
                    ToggleHands = false;
                    ToggleLegs = false;
                    ToggleFeet = false;
                    ToggleNeck = false;
                    ToggleRing = false;
                    ToggleMainHand = false;
                    ToggleRanged = false;
                    ToggleOffHand = false;
                }

                RaisePropertyChanged();
                ItemAffixesFiltered?.Refresh();
                ItemAffixesActiveFiltered?.Refresh();
                ItemAspectsFiltered?.Refresh();
                ItemAspectsActiveFiltered?.Refresh();
                ItemTypesFiltered?.Refresh();
            }
        }
        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            ApplicationLoadedExecute();
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

        private void HandleReloadAffixesGuiRequestEvent()
        {
            ApplicationLoadedExecute();
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsAffixOverlayEnabled = toggleOverlayEventParams.IsEnabled;
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void ApplicationLoadedExecute()
        {
            // Load affix presets
            UpdateAffixPresets();

            // Load item affixes active
            UpdateItemAffixesActive();

            // Load item affixes
            UpdateItemAffixes();

            // Load item aspects active
            UpdateItemAspectsActive();

            // Load item aspects
            UpdateItemAspects();

            // Load item types
            UpdateItemTypes();
        }

        private bool CanAddAffixPresetNameExecute()
        {
            return !string.IsNullOrWhiteSpace(AffixPresetName) &&
                !_affixPresets.Any(preset => preset.Name.Equals(AffixPresetName));
        }

        private void AddAffixPresetNameExecute()
        {
            _affixPresetManager.AddAffixPreset(new AffixPreset
            {
                Name = AffixPresetName
            });
        }

        private bool CanRemoveAffixPresetNameExecute()
        {
            return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
        }

        private void RemoveAffixPresetNameExecute()
        {
            _affixPresetManager.RemoveAffixPreset(SelectedAffixPreset);
        }

        private void ActiveAffixDoubleClickedExecute(object itemAffixObj)
        {
            ItemAffix itemAffix = (ItemAffix)itemAffixObj;

            if (SelectedAffixPreset != null)
            {
                SelectedAffixPreset.ItemAffixes.Remove(itemAffix);
                UpdateItemAffixesActive();
                _affixPresetManager.SaveAffixPresets();
                ItemAffixesFiltered?.Refresh();
            }
        }

        private void InactiveAffixDoubleClickedExecute(object itemAffixObj)
        {
            ItemAffix itemAffix = (ItemAffix)itemAffixObj;
            string currentItemType = GetCurrentItemType();

            if (SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(currentItemType))
            {
                if (!ItemAffixesActive.Any(affix => affix.FileName.Equals(itemAffix.FileName) && affix.Type.Equals(currentItemType)))
                {
                    SelectedAffixPreset.ItemAffixes.Add(new ItemAffix
                    {
                        FileName = itemAffix.FileName,
                        Type = currentItemType
                    });
                    UpdateItemAffixesActive();
                    _affixPresetManager.SaveAffixPresets();
                    ItemAffixesFiltered?.Refresh();
                }
            }
        }

        private void ActiveAspectDoubleClickedExecute(object itemAspectObj)
        {
            ItemAspect itemAspect = (ItemAspect)itemAspectObj;

            if (SelectedAffixPreset != null)
            {
                SelectedAffixPreset.ItemAspects.Remove(itemAspect);
                UpdateItemAspectsActive();
                _affixPresetManager.SaveAffixPresets();
                ItemAspectsFiltered?.Refresh();
            }
        }

        private void InactiveAspectDoubleClickedExecute(object itemAspectObj)
        {
            ItemAspect itemAspect = (ItemAspect)itemAspectObj;
            string currentItemType = GetCurrentItemType();

            if (SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(currentItemType))
            {
                if (!ItemAspectsActive.Any(aspect => aspect.FileName.Equals(itemAspect.FileName) && aspect.Type.Equals(currentItemType)))
                {
                    SelectedAffixPreset.ItemAspects.Add(new ItemAspect
                    {
                        FileName = itemAspect.FileName,
                        Type = currentItemType
                    });
                    UpdateItemAspectsActive();
                    _affixPresetManager.SaveAffixPresets();
                    ItemAspectsFiltered?.Refresh();
                }
            }
        }

        private void CreateItemAffixesFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ItemAffixesFiltered = new ListCollectionView(ItemAffixes)
                {
                    Filter = FilterItemAffixes
                };
            });
        }

        private bool FilterItemAffixes(object itemAffixObj)
        {
            var allowed = true;
            if (itemAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)itemAffixObj;
            string currentItemType = GetCurrentItemType();

            if (ItemAffixesActive.Any(affix => affix.FileName.Equals(itemAffix.FileName) && affix.Type.Equals(currentItemType)))
            {
                allowed = false;
            }

            if (!itemAffix.FileName.ToLower().Contains(AffixNameFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixNameFilter))
            {
                allowed = false;
            }

            // Extra filter for the sigils
            if ((itemAffix.FileName.ToLower().StartsWith(ItemTypeConstants.Sigil) && !currentItemType.Equals(ItemTypeConstants.Sigil)) ||
                (!itemAffix.FileName.ToLower().StartsWith(ItemTypeConstants.Sigil) && currentItemType.Equals(ItemTypeConstants.Sigil)))
            {
                allowed = false;
            }

            return allowed;
        }

        private void CreateItemAffixesActiveFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ItemAffixesActiveFiltered = new ListCollectionView(ItemAffixesActive)
                {
                    Filter = FilterItemAffixesActive
                };
            });
        }

        private bool FilterItemAffixesActive(object itemAffixObj)
        {
            var allowed = false;
            if (itemAffixObj == null) return false;

            ItemAffix itemAffix = (ItemAffix)itemAffixObj;

            switch (itemAffix.Type)
            {
                case ItemTypeConstants.Helm:
                    allowed = ToggleHead;
                    break;
                case ItemTypeConstants.Chest:
                    allowed = ToggleTorso;
                    break;
                case ItemTypeConstants.Gloves:
                    allowed = ToggleHands;
                    break;
                case ItemTypeConstants.Pants:
                    allowed = ToggleLegs;
                    break;
                case ItemTypeConstants.Boots:
                    allowed = ToggleFeet;
                    break;
                case ItemTypeConstants.Amulet:
                    allowed = ToggleNeck;
                    break;
                case ItemTypeConstants.Ring:
                    allowed = ToggleRing;
                    break;
                case ItemTypeConstants.Weapon:
                    allowed = ToggleMainHand;
                    break;
                case ItemTypeConstants.Ranged:
                    allowed = ToggleRanged;
                    break;
                case ItemTypeConstants.Offhand:
                    allowed = ToggleOffHand;
                    break;
                case ItemTypeConstants.Sigil:
                    allowed = ToggleSigil;
                    break;
                default:
                    allowed = false;
                    break;
            }

            return allowed;
        }

        private void CreateItemAspectsFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ItemAspectsFiltered = new ListCollectionView(ItemAspects)
                {
                    Filter = FilterItemAspects
                };
            });
        }

        private bool FilterItemAspects(object itemAspectObj)
        {
            var allowed = true;
            if (itemAspectObj == null) return false;

            ItemAspect itemAspect = (ItemAspect)itemAspectObj;
            string currentItemType = GetCurrentItemType();

            if (ItemAspectsActive.Any(aspect => aspect.FileName.Equals(itemAspect.FileName) && aspect.Type.Equals(currentItemType)))
            {
                allowed = false;
            }

            if (!itemAspect.FileName.ToLower().Contains(AspectNameFilter.ToLower()) && !string.IsNullOrWhiteSpace(AspectNameFilter))
            {
                allowed = false;
            }

            return allowed;
        }

        private void CreateItemAspectsActiveFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ItemAspectsActiveFiltered = new ListCollectionView(ItemAspectsActive)
                {
                    Filter = FilterItemAspectsActive
                };
            });
        }

        private bool FilterItemAspectsActive(object itemAspectsObj)
        {
            var allowed = false;
            if (itemAspectsObj == null) return false;

            ItemAspect itemAspect = (ItemAspect)itemAspectsObj;

            switch (itemAspect.Type)
            {
                case ItemTypeConstants.Helm:
                    allowed = ToggleHead;
                    break;
                case ItemTypeConstants.Chest:
                    allowed = ToggleTorso;
                    break;
                case ItemTypeConstants.Gloves:
                    allowed = ToggleHands;
                    break;
                case ItemTypeConstants.Pants:
                    allowed = ToggleLegs;
                    break;
                case ItemTypeConstants.Boots:
                    allowed = ToggleFeet;
                    break;
                case ItemTypeConstants.Amulet:
                    allowed = ToggleNeck;
                    break;
                case ItemTypeConstants.Ring:
                    allowed = ToggleRing;
                    break;
                case ItemTypeConstants.Weapon:
                    allowed = ToggleMainHand;
                    break;
                case ItemTypeConstants.Ranged:
                    allowed = ToggleRanged;
                    break;
                case ItemTypeConstants.Offhand:
                    allowed = ToggleOffHand;
                    break;
                case ItemTypeConstants.Sigil:
                    allowed = ToggleOffHand;
                    break;
                default:
                    allowed = false;
                    break;
            }

            return allowed;
        }

        private void CreateItemTypeFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ItemTypesFiltered = new ListCollectionView(ItemTypes)
                {
                    Filter = FilterItemTypes
                };
            });
        }

        private bool FilterItemTypes(object itemTypesObj)
        {
            var allowed = false;
            if (itemTypesObj == null) return false;

            ItemType itemType = (ItemType)itemTypesObj;

            switch (itemType.Name)
            {
                case ItemTypeConstants.Helm:
                    allowed = ToggleHead;
                    break;
                case ItemTypeConstants.Chest:
                    allowed = ToggleTorso;
                    break;
                case ItemTypeConstants.Gloves:
                    allowed = ToggleHands;
                    break;
                case ItemTypeConstants.Pants:
                    allowed = ToggleLegs;
                    break;
                case ItemTypeConstants.Boots:
                    allowed = ToggleFeet;
                    break;
                case ItemTypeConstants.Amulet:
                    allowed = ToggleNeck;
                    break;
                case ItemTypeConstants.Ring:
                    allowed = ToggleRing;
                    break;
                case ItemTypeConstants.Weapon:
                    allowed = ToggleMainHand;
                    break;
                case ItemTypeConstants.Ranged:
                    allowed = ToggleRanged;
                    break;
                case ItemTypeConstants.Offhand:
                    allowed = ToggleOffHand;
                    break;
                case ItemTypeConstants.Sigil:
                    allowed = ToggleSigil;
                    break;
                default:
                    allowed = false;
                    break;
            }

            return allowed;
        }

        private string GetCurrentItemType()
        {
            string currentItemType = string.Empty;

            if (ToggleHead)
            {
                currentItemType = ItemTypeConstants.Helm;
            }
            else if (ToggleTorso)
            {
                currentItemType = ItemTypeConstants.Chest;
            }
            else if (ToggleHands)
            {
                currentItemType = ItemTypeConstants.Gloves;
            }
            else if (ToggleLegs)
            {
                currentItemType = ItemTypeConstants.Pants;
            }
            else if (ToggleFeet)
            {
                currentItemType = ItemTypeConstants.Boots;
            }
            else if (ToggleNeck)
            {
                currentItemType = ItemTypeConstants.Amulet;
            }
            else if (ToggleRing)
            {
                currentItemType = ItemTypeConstants.Ring;
            }
            else if (ToggleMainHand)
            {
                currentItemType = ItemTypeConstants.Weapon;
            }
            else if (ToggleRanged)
            {
                currentItemType = ItemTypeConstants.Ranged;
            }
            else if (ToggleOffHand)
            {
                currentItemType = ItemTypeConstants.Offhand;
            }
            else if (ToggleSigil)
            {
                currentItemType = ItemTypeConstants.Sigil;
            }

            return currentItemType;
        }

        private void LoadItemTypeIcons()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = string.Empty;

            resourcePath = "head_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageHead = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "torso_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageTorso = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "hands_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageHands = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "legs_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageLegs = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "feet_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageFeet = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "neck_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageNeck = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "ring_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageRing = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "mainhand_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageMainHand = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "ranged_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageRanged = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "offhand_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    ImageOffHand = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }

            resourcePath = "sigil_icon.png";
            resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourcePath));
            using (Stream? stream = assembly.GetManifestResourceStream(resourcePath)) {
                if (stream != null) {
                    ImageSigil = Helpers.ScreenCapture.ImageSourceFromBitmap(new Bitmap(stream));
                }
            }
        }

        private void UpdateAffixPresets()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AffixPresets.Clear();
                    AffixPresets.AddRange(_affixPresetManager.AffixPresets);
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
        }

        private void UpdateItemAffixes()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ItemAffixes.Clear();
                    ItemAffixes.AddRange(_affixPresetManager.ItemAffixes);
                });
            }
        }

        private void UpdateItemAffixesActive()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ItemAffixesActive.Clear();
                    if (SelectedAffixPreset != null)
                    {
                        ItemAffixesActive.AddRange(SelectedAffixPreset.ItemAffixes);
                    }
                });
            }
        }

        private void UpdateItemAspects()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ItemAspects.Clear();
                    ItemAspects.AddRange(_affixPresetManager.ItemAspects);
                });
            }
        }

        private void UpdateItemAspectsActive()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ItemAspectsActive.Clear();
                    if (SelectedAffixPreset != null)
                    {
                        ItemAspectsActive.AddRange(SelectedAffixPreset.ItemAspects);
                    }
                });
            }
        }

        private void UpdateItemTypes()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ItemTypes.Clear();
                    if (_settingsManager.Settings.LiteMode)
                    {
                        ItemTypes.AddRange(_affixPresetManager.ItemTypesLite);
                    }
                    else
                    {
                        ItemTypes.AddRange(_affixPresetManager.ItemTypes);
                    }
                });
            }
        }

        #endregion
    }
}
