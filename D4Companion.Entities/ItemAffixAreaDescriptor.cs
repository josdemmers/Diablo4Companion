using System.Drawing;

namespace D4Companion.Entities
{
    public class ItemAffixAreaDescriptor
    {
        public Rectangle Location { get; set; } = Rectangle.Empty;
        public string AffixType { get; set; } = string.Empty;
    }
}
