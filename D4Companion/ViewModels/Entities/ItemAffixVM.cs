using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Services;
using Prism.Events;
using Prism.Mvvm;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffixVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private ItemAffix _itemAffix = new ItemAffix();

        // Start of Constructors region

        #region Constructors

        public ItemAffixVM(ItemAffix itemAffix)
        {
            _itemAffix = itemAffix;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public string Id
        {
            get => _itemAffix.Id;
        }

        public string Type
        {
            get => _itemAffix.Type;
        }

        public string TypeIcon
        {
            get
            {
                switch (Type)
                {
                    case Constants.ItemTypeConstants.Amulet:
                        return "/Images/neck_icon.png";
                    case Constants.ItemTypeConstants.Boots:
                        return "/Images/feet_icon.png";
                    case Constants.ItemTypeConstants.Chest:
                        return "/Images/torso_icon.png";
                    case Constants.ItemTypeConstants.Gloves:
                        return "/Images/hands_icon.png";
                    case Constants.ItemTypeConstants.Helm:
                        return "/Images/head_icon.png";
                    case Constants.ItemTypeConstants.Offhand:
                        return "/Images/offhand_icon.png";
                    case Constants.ItemTypeConstants.Pants:
                        return "/Images/legs_icon.png";
                    case Constants.ItemTypeConstants.Ranged:
                        return "/Images/ranged_icon.png";
                    case Constants.ItemTypeConstants.Ring:
                        return "/Images/ring_icon.png";
                    case Constants.ItemTypeConstants.Weapon:
                        return "/Images/mainhand_icon.png";
                    default:
                        return string.Empty;
                }
            }
        }

        public Color Color
        {
            get => _itemAffix.Color;
            set => _itemAffix.Color = value;
        }

        public bool IsAnyType
        {
            get => _itemAffix.IsAnyType;
            set
            {
                _itemAffix.IsAnyType = value;
                RaisePropertyChanged(nameof(IsAnyType));

                _affixManager.SetIsAnyType(Model, value);

                _affixManager.SaveAffixPresets();
                _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
            }
        }

        public bool IsDuplicate
        {
            get
            {
                return _affixManager.IsDuplicate(Model);
            }
        }

        public bool IsGreater
        {
            get => _itemAffix.IsGreater;
            set
            {
                _itemAffix.IsGreater = value;
                RaisePropertyChanged(nameof(IsGreater));
                RaisePropertyChanged(nameof(IsDuplicate));

                if (IsGreater)
                {
                    IsImplicit = false;
                    IsTempered = false;
                    Color = _settingsManager.Settings.DefaultColorGreater;
                }
                else if (!IsImplicit && !IsGreater && !IsTempered)
                {
                    Color = _settingsManager.Settings.DefaultColorNormal;
                }

                _affixManager.SaveAffixPresets();
                _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
            }
        }

        public bool IsImplicit
        {
            get => _itemAffix.IsImplicit;
            set
            {
                _itemAffix.IsImplicit = value;
                RaisePropertyChanged(nameof(IsImplicit));
                RaisePropertyChanged(nameof(IsDuplicate));

                if (IsImplicit)
                {
                    IsGreater = false;
                    IsTempered = false;
                    Color = _settingsManager.Settings.DefaultColorImplicit;
                }
                else if (!IsImplicit && !IsGreater && !IsTempered)
                {
                    Color = _settingsManager.Settings.DefaultColorNormal;
                }

                _affixManager.SaveAffixPresets();
                _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
            }
        }

        public bool IsTempered
        {
            get => _itemAffix.IsTempered;
            set
            {
                _itemAffix.IsTempered = value;
                RaisePropertyChanged(nameof(IsTempered));
                RaisePropertyChanged(nameof(IsDuplicate));

                if (IsTempered)
                {
                    IsGreater = false;
                    IsImplicit = false;
                    Color = _settingsManager.Settings.DefaultColorTempered;
                }
                else if (!IsImplicit && !IsGreater && !IsTempered)
                {
                    Color = _settingsManager.Settings.DefaultColorNormal;
                }

                _affixManager.SaveAffixPresets();
                _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
            }
        }

        public ItemAffix Model
        {
            get => _itemAffix;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
