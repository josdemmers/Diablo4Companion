namespace D4Companion.Entities
{
    public class TradeItem
    {
        public List<ItemAffix> Affixes { get; set; } = new List<ItemAffix>();
        public TradeItemType Type { get; set; } = new TradeItemType();
        public string Value { get; set; } = string.Empty;
    }

    public class TradeItemType
    {
        public string Image { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
