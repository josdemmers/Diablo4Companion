using Prism.Mvvm;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffixTradeVM : BindableBase
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
                RaisePropertyChanged(nameof(Id));
            }
        }

        public string Type
        {
            get => _itemAffix.Type;
            set
            {
                _itemAffix.Type = value;
                RaisePropertyChanged(nameof(Type));
            }
        }

        public Color Color
        {
            get => _itemAffix.Color;
            set
            {
                _itemAffix.Color = value;
                RaisePropertyChanged(nameof(Color));
            }
        }

        public bool IsGreater
        {
            get => _itemAffix.IsGreater;
            set
            {
                _itemAffix.IsGreater = value;
                RaisePropertyChanged(nameof(IsGreater));
            }
        }

        public bool IsImplicit
        {
            get => _itemAffix.IsImplicit;
            set
            {
                _itemAffix.IsImplicit = value;
                RaisePropertyChanged(nameof(IsImplicit));
            }
        }

        public bool IsTempered
        {
            get => _itemAffix.IsTempered;
            set
            {
                _itemAffix.IsTempered = value;
                RaisePropertyChanged(nameof(IsTempered));
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
