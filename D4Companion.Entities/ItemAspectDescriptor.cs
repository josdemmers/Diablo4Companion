using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ItemAspectDescriptor
    {
        public ItemAffix ItemAspect { get; set; } = new ItemAffix();
        public string ItemAspectMappedImage { get; set; } = string.Empty; // TODO: Remove
        public double Similarity { get; set; } = 1; // TODO: Remove
        public Rectangle Location { get; set; } = Rectangle.Empty; // TODO: Remove
    }
}
