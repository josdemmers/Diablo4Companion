using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ItemTooltipDescriptor
    {
        public double Similarity { get; set; } = 1;
        public string ItemType { get; set; } = string.Empty;
        public Rectangle Location { get; set; } = Rectangle.Empty;
        /// <summary>
        /// Location of preferred affixes.
        /// </summary>
        public List<Rectangle> ItemAffixes { get; set; } = new List<Rectangle>();
        /// <summary>
        /// Location of all affixes.
        /// </summary>
        public List<Rectangle> ItemAffixLocations { get; set; } = new List<Rectangle>();
        /// <summary>
        /// Location of preferred aspect.
        /// </summary>
        public Rectangle ItemAspect { get; set; } = new Rectangle();
        /// <summary>
        /// Location of aspect.
        /// </summary>
        public Rectangle ItemAspectLocation { get; set; } = new Rectangle();
    }
}
