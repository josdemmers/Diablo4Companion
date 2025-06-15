using D4Companion.Constants;
using D4Companion.Entities;
using Prism.Mvvm;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class TradeItemBase : BindableBase
    {

    }

    public class TradeItemAdd : TradeItemBase
    {

    }

    public class TradeItemCurrent : TradeItemBase
    {
        private string _affixes = string.Empty;
        private string _itemPower = string.Empty;
        private string _itemType = string.Empty;

        public string Affixes
        {
            get => _affixes;
            set
            {
                _affixes = value;
                RaisePropertyChanged(nameof(Affixes));
            }
        }

        public string ItemType
        {
            get => _itemType;
            set
            {
                _itemType = value;
                RaisePropertyChanged(nameof(ItemType));
            }
        }

        public string ItemPower
        {
            get => _itemPower;
            set
            {
                _itemPower = value;
                RaisePropertyChanged(nameof(ItemPower));
            }
        }
    }

    public class TradeItemWanted : TradeItemBase
    {
        private ObservableCollection<ItemAffixTradeVM> _affixes = new ObservableCollection<ItemAffixTradeVM>();
        private string _value = "0";

        // Start of Constructors region

        #region Constructors

        public TradeItemWanted()
        {
            
        }

        public TradeItemWanted(TradeItem tradeItem)
        {
            Type = tradeItem.Type;
            Value = tradeItem.Value;

            Affixes.AddRange(tradeItem.Affixes.Select(itemAffix => new ItemAffixTradeVM(itemAffix)));
        }

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<ItemAffixTradeVM> Affixes { get => _affixes; set => _affixes = value; }

        public bool IsItemTypeRune
        {
            get => Type.Type.Equals(ItemTypeConstants.Rune);
        }

        public TradeItemType Type { get; set; } = new TradeItemType();

        public string Value
        {
            get => _value;
            set
            {
                _value = string.IsNullOrWhiteSpace(value) ? "0" : value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        public TradeItem AsTradeItem()
        {
            return new TradeItem
            {
                Affixes = Affixes.Select(affix => affix.Model).ToList(),
                Type = Type,
                Value = Value
            };
        }

        #endregion
    }

    public class TradeItemCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if ((x.GetType() == typeof(TradeItemAdd)) && !(y.GetType() == typeof(TradeItemAdd))) return -1;
            if ((y.GetType() == typeof(TradeItemAdd)) && !(x.GetType() == typeof(TradeItemAdd))) return 1;

            if ((x.GetType() == typeof(TradeItemCurrent)) && !(y.GetType() == typeof(TradeItemCurrent))) return -1;
            if ((y.GetType() == typeof(TradeItemCurrent)) && !(x.GetType() == typeof(TradeItemCurrent))) return 1;

            if ((x.GetType() == typeof(TradeItemWanted)) && (y.GetType() == typeof(TradeItemWanted)))
            {
                var itemX = (TradeItemWanted)x;
                var itemY = (TradeItemWanted)y;

                result = itemX.Type.Name.CompareTo(itemY.Type.Name);
                if (result == 0)
                {
                    string valueX = itemX.Value;
                    string valueY = itemY.Value;

                    while (valueX.Length < valueY.Length)
                    {
                        valueX = "0" + valueX;
                    }
                    while (valueY.Length < valueX.Length)
                    {
                        valueY = "0" + valueY;
                    }

                    result = valueX.CompareTo(valueY);
                }
            }

            return result;
        }
    }
}
