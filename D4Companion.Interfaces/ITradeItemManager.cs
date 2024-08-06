using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface ITradeItemManager
    {
        List<TradeItem> TradeItems { get; }

        TradeItem? FindTradeItem(string itemType, List<ItemAffix> affixes);
        void SaveTradeItems(List<TradeItem> tradeItems);
    }
}
