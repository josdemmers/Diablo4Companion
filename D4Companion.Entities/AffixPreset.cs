namespace D4Companion.Entities
{
    public class AffixPreset
    {
        public string Name { get; set; } = string.Empty;
        public List<ItemAffix> ItemAffixes { get; set; } = new List<ItemAffix>();
        public List<ItemAffix> ItemAspects { get; set; } = new List<ItemAffix>();
        public List<ItemAffix> ItemSigils { get; set; } = new List<ItemAffix>();
        public List<ItemAffix> ItemUniques { get; set; } = new List<ItemAffix>();
        public List<ItemAffix> ItemRunes { get; set; } = new List<ItemAffix>();
        public List<List<ParagonBoard>> ParagonBoardsList { get; set; } = new List<List<ParagonBoard>>();

        public AffixPreset Clone()
        {
            return (AffixPreset)this.MemberwiseClone();
        }
    }
}