using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffixVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAffixManager _affixManager;

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
        }

        public bool IsGreater
        {
            get => _itemAffix.IsGreater;
            set
            {
                _itemAffix.IsGreater = value;
                RaisePropertyChanged(nameof(IsGreater));

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
