namespace D4Companion.Entities
{
    public class AffixGlobal
    {
        public List<PtContent> ptContent { get; set; } = new List<PtContent>();
    }

    public class PtContent
    {
        public List<ArSortedAffixGroups> arSortedAffixGroups { get; set; } = [];
    }

    public class ArSortedAffixGroups
    {
        public List<ArSortedAffixes> arSortedAffixes { get; set; } = [];
    }

    public class ArSortedAffixes
    {
        public int __raw__ { get; set; }
        public string name { get; set; } = string.Empty;
    }
}
