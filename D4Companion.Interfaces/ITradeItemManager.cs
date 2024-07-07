using D4Companion.Entities;
namespace D4Companion.Interfaces
{
    public interface ITradeItemManager
    {
        List<TradeItem> TradeItems { get; }

        void SaveTradeItems(List<TradeItem> tradeItems);
    }
}
