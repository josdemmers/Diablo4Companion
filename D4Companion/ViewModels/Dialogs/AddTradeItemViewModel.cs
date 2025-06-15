using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.ViewModels.Entities;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.ViewModels.Dialogs
{
    public class AddTradeItemViewModel : BindableBase
    {
        private readonly IAffixManager _affixManager;

        private ObservableCollection<AffixInfoWanted> _affixes = new ObservableCollection<AffixInfoWanted>();
        private ObservableCollection<RuneInfoWanted> _affixesRunes = new ObservableCollection<RuneInfoWanted>();
        private ObservableCollection<TradeItemType> _itemTypes = new ObservableCollection<TradeItemType>();
        public ListCollectionView? AffixesFiltered { get; private set; }
        public ListCollectionView? AffixesRunesFiltered { get; private set; }

        private string _affixTextFilter = string.Empty;
        private bool _editMode = false;
        private TradeItemType _selectedItemType = new TradeItemType();
        private TradeItemWanted _tradeItem = new TradeItemWanted();

        // Start of Constructors region

        #region Constructors

        public AddTradeItemViewModel(Action<AddTradeItemViewModel> closeHandler, TradeItemWanted tradeItem, bool editMode)
        {
            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));

            // Init View commands
            AddAffixCommand = new DelegateCommand<AffixInfoWanted>(AddAffixExecute);
            AddAffixRuneCommand = new DelegateCommand<RuneInfoWanted>(AddAffixRuneExecute, CanAddAffixRuneExecute);
            CloseCommand = new DelegateCommand<AddTradeItemViewModel>(closeHandler);
            RemoveAffixCommand = new DelegateCommand<ItemAffixTradeVM>(RemoveAffixExecute);
            SetCancelCommand = new DelegateCommand(SetCancelExecute);
            SetDoneCommand = new DelegateCommand(SetDoneExecute);

            _editMode = editMode;
            _tradeItem = tradeItem;

            // Init collections
            InitAffixes();
            InitAffixesRunes();
            InitItemTypes();

            // Create filter views
            CreateItemAffixesFilteredView();
            CreateItemAffixesRunesFilteredView();

            // Init events for existing affixes
            InitTradeItemAffixEvents();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfoWanted> Affixes { get => _affixes; set => _affixes = value; }
        public ObservableCollection<RuneInfoWanted> AffixesRunes { get => _affixesRunes; set => _affixesRunes = value; }
        public ObservableCollection<TradeItemType> ItemTypes { get => _itemTypes; set => _itemTypes = value; }

        public DelegateCommand<AffixInfoWanted> AddAffixCommand { get; }
        public DelegateCommand<RuneInfoWanted> AddAffixRuneCommand { get; }
        public DelegateCommand<AddTradeItemViewModel> CloseCommand { get; }
        public DelegateCommand<ItemAffixTradeVM> RemoveAffixCommand { get; }
        public DelegateCommand SetCancelCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public bool IsCanceled { get; set; } = false;

        public string AffixTextFilter
        {
            get => _affixTextFilter;
            set
            {
                SetProperty(ref _affixTextFilter, value, () => { RaisePropertyChanged(nameof(AffixTextFilter)); });
                AffixesFiltered?.Refresh();
                AffixesRunesFiltered?.Refresh();
            }
        }

        public TradeItemType SelectedItemType
        {
            get => _selectedItemType;
            set
            {
                string previousType = _selectedItemType.Type;

                _selectedItemType = value;
                TradeItem.Type = value;

                if (!string.IsNullOrWhiteSpace(previousType) &&
                    ((previousType.Equals(ItemTypeConstants.Rune) && !_selectedItemType.Type.Equals(ItemTypeConstants.Rune)) ||
                    (!previousType.Equals(ItemTypeConstants.Rune) && _selectedItemType.Type.Equals(ItemTypeConstants.Rune))))
                {
                    TradeItem.Affixes.Clear();
                    AddAffixRuneCommand.RaiseCanExecuteChanged();
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsItemTypeRune));
                RaisePropertyChanged(nameof(TradeItem));
            }
        }

        public bool EditMode
        {
            get => _editMode;
            set
            {
                _editMode = value;
                RaisePropertyChanged();
            }
        }

        public bool IsItemTypeRune
        {
            get => _selectedItemType.Type.Equals(ItemTypeConstants.Rune);
        }

        public TradeItemWanted TradeItem
        {
            get => _tradeItem;
            set
            {
                _tradeItem = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AddAffixExecute(AffixInfoWanted affixInfoVM)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (affixInfoVM != null)
                {
                    var itemAffixTradeVM = new ItemAffixTradeVM(new ItemAffix
                    {
                        Id = affixInfoVM.IdName,
                        Type = Constants.ItemTypeConstants.Amulet
                    });
                    itemAffixTradeVM.PropertyChanged += ItemAffixPropertyChangedEventHandler;
                    TradeItem.Affixes.Add(itemAffixTradeVM);
                    TradeItem.Affixes = new ObservableCollection<ItemAffixTradeVM>(TradeItem.Affixes.OrderBy(a => a.Id));
                    RaisePropertyChanged(nameof(TradeItem));
                }
            });
        }

        private bool CanAddAffixRuneExecute(RuneInfoWanted runeInfoVM)
        {
            return TradeItem.Affixes.Count == 0;
        }

        private void AddAffixRuneExecute(RuneInfoWanted runeInfoVM)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (runeInfoVM != null)
                {
                    var itemAffixTradeVM = new ItemAffixTradeVM(new ItemAffix
                    {
                        Id = runeInfoVM.IdName,
                        Type = Constants.ItemTypeConstants.Rune
                    });
                    itemAffixTradeVM.PropertyChanged += ItemAffixPropertyChangedEventHandler;
                    TradeItem.Affixes.Add(itemAffixTradeVM);
                    TradeItem.Affixes = new ObservableCollection<ItemAffixTradeVM>(TradeItem.Affixes.OrderBy(a => a.Id));
                    RaisePropertyChanged(nameof(TradeItem));

                    AddAffixRuneCommand.RaiseCanExecuteChanged();
                }
            });
        }

        private void ItemAffixPropertyChangedEventHandler(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Add/Remove to update ObservableCollection
            var currentList = TradeItem.Affixes.ToList();
            TradeItem.Affixes.Clear();
            TradeItem.Affixes.AddRange(currentList);
        }

        private void RemoveAffixExecute(ItemAffixTradeVM itemAffix)
        {
            if (itemAffix != null)
            {
                TradeItem.Affixes.Remove(itemAffix);
                AddAffixRuneCommand?.RaiseCanExecuteChanged();
            }
        }

        private void SetCancelExecute()
        {
            IsCanceled = true;
            CloseCommand.Execute(this);
        }

        private void SetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void CreateItemAffixesFilteredView()
        {
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
            if (affixObj == null) return false;

            AffixInfoWanted affixInfoVM = (AffixInfoWanted)affixObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!affixInfoVM.Description.ToLower().Contains(keyword.Trim().ToLower()))
                {
                    return false;
                }
            }

            return true;
        }

        private void CreateItemAffixesRunesFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AffixesRunesFiltered = new ListCollectionView(AffixesRunes)
                {
                    Filter = FilterAffixesRunes
                };
            });
        }

        private bool FilterAffixesRunes(object affixObj)
        {
            if (affixObj == null) return false;

            RuneInfoWanted affixInfoVM = (RuneInfoWanted)affixObj;

            var keywords = AffixTextFilter.Split(";");
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;

                if (!affixInfoVM.Name.ToLower().Contains(keyword.Trim().ToLower()))
                {
                    return false;
                }
            }

            return true;
        }

        private void InitAffixes()
        {
            Affixes.Clear();
            Affixes.AddRange(_affixManager.Affixes.Select(affixInfo => new AffixInfoWanted(affixInfo)));
        }

        private void InitAffixesRunes()
        {
            AffixesRunes.Clear();
            AffixesRunes.AddRange(_affixManager.Runes.Select(runeInfo => new RuneInfoWanted(runeInfo)));
        }

        private void InitItemTypes()
        {
            ItemTypes.Clear();
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Amulet, Name = TranslationSource.Instance["rsCapAmulet"], Image = "/Images/neck_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Boots, Name = TranslationSource.Instance["rsCapBoots"], Image = "/Images/feet_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Chest, Name = TranslationSource.Instance["rsCapChest"], Image = "/Images/torso_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Gloves, Name = TranslationSource.Instance["rsCapGloves"], Image = "/Images/hands_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Helm, Name = TranslationSource.Instance["rsCapHelm"], Image = "/Images/head_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Offhand, Name = TranslationSource.Instance["rsCapOffhand"], Image = "/Images/offhand_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Pants, Name = TranslationSource.Instance["rsCapPants"], Image = "/Images/legs_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Ranged, Name = TranslationSource.Instance["rsCapRanged"], Image = "/Images/ranged_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Ring, Name = TranslationSource.Instance["rsCapRing"], Image = "/Images/ring_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Rune, Name = TranslationSource.Instance["rsCapRune"], Image = "/Images/rune_icon.png" });
            ItemTypes.Add(new TradeItemType() { Type = ItemTypeConstants.Weapon, Name = TranslationSource.Instance["rsCapWeapon"], Image = "/Images/mainhand_icon.png" });

            SelectedItemType = _tradeItem?.Type != null ? ItemTypes.FirstOrDefault(i => i.Image.Equals(_tradeItem.Type.Image)) ?? ItemTypes[0] : ItemTypes[0];
        }

        private void InitTradeItemAffixEvents()
        {
            foreach (var affix in TradeItem.Affixes)
            {
                affix.PropertyChanged += ItemAffixPropertyChangedEventHandler;
            }
        }

        #endregion
    }
}