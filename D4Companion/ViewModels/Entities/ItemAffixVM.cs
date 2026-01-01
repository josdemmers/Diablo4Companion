using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffixVM : ObservableObject
    {
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private ItemAffix _itemAffix = new ItemAffix();

        // Start of Constructors region

        #region Constructors

        public ItemAffixVM(ItemAffix itemAffix)
        {
            _itemAffix = itemAffix;

            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();
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
                OnPropertyChanged(nameof(IsAnyType));

                _affixManager.SetIsAnyType(Model, value);

                _affixManager.SaveAffixPresets();
                WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
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
                OnPropertyChanged(nameof(IsGreater));
                OnPropertyChanged(nameof(IsDuplicate));

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
                WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
            }
        }

        public bool IsImplicit
        {
            get => _itemAffix.IsImplicit;
            set
            {
                _itemAffix.IsImplicit = value;
                OnPropertyChanged(nameof(IsImplicit));
                OnPropertyChanged(nameof(IsDuplicate));

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
                WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
            }
        }

        public bool IsTempered
        {
            get => _itemAffix.IsTempered;
            set
            {
                _itemAffix.IsTempered = value;
                OnPropertyChanged(nameof(IsTempered));
                OnPropertyChanged(nameof(IsDuplicate));

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
                WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
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
