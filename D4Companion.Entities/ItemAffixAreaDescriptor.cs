using System.Drawing;

namespace D4Companion.Entities
{
    public class ItemAffixAreaDescriptor
    {
        public Rectangle Location { get; set; } = Rectangle.Empty;
        public string AffixType { get; set; } = string.Empty;
        public double AffixValue { get; set; } = 0.0;
        public double AffixThresholdValue { get; set; } = 0.0;
    }
}
