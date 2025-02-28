using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface ITradeItemManager
    {
        List<TradeItem> TradeItems { get; }

        TradeItem? FindTradeItem(string itemType, List<Tuple<int, ItemAffix>> affixes, List<ItemAffixAreaDescriptor> affixAreas);
        void SaveTradeItems(List<TradeItem> tradeItems);
    }
}
