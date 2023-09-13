using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ItemAffixDescriptor
    {
        public int AreaIndex { get; set; } = 0;
        public ItemAffix ItemAffix { get; set; } = new ItemAffix();
        public string ItemAffixMappedImage { get; set; } = string.Empty;
        public double Similarity { get; set; } = 1;
        public Rectangle Location { get; set; } = Rectangle.Empty;
    }
}
