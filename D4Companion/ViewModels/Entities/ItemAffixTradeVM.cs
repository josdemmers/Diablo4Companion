using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffixTradeVM : ObservableObject
    {
        private ItemAffix _itemAffix = new ItemAffix();

        // Start of Constructors region

        #region Constructors

        public ItemAffixTradeVM(ItemAffix itemAffix)
        {
            _itemAffix = itemAffix;
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
            set
            {
                _itemAffix.Id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Type
        {
            get => _itemAffix.Type;
            set
            {
                _itemAffix.Type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public Color Color
        {
            get => _itemAffix.Color;
            set
            {
                _itemAffix.Color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        public bool IsGreater
        {
            get => _itemAffix.IsGreater;
            set
            {
                _itemAffix.IsGreater = value;
                OnPropertyChanged(nameof(IsGreater));
            }
        }

        public bool IsImplicit
        {
            get => _itemAffix.IsImplicit;
            set
            {
                _itemAffix.IsImplicit = value;
                OnPropertyChanged(nameof(IsImplicit));
            }
        }

        public bool IsTempered
        {
            get => _itemAffix.IsTempered;
            set
            {
                _itemAffix.IsTempered = value;
                OnPropertyChanged(nameof(IsTempered));
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
