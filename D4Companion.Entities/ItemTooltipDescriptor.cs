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
        public bool IsUniqueItem { get; set; } = false;
        /// <summary>
        /// List of detected affixes.
        /// </summary>
        public List<Tuple<int,ItemAffix>> ItemAffixes { get; set; } = new List<Tuple<int, ItemAffix>>();
        /// <summary>
        /// Areas containing an affix.
        /// </summary>
        public List<Rectangle> ItemAffixAreas { get; set; } = new List<Rectangle>();
        /// <summary>
        /// Location of all affixes.
        /// </summary>
        public List<Rectangle> ItemAffixLocations { get; set; } = new List<Rectangle>();
        /// <summary>
        /// Detected aspect.
        /// </summary>
        public ItemAffix ItemAspect { get; set; } = new ItemAffix();
        /// <summary>
        /// Area containing an aspect.
        /// </summary>
        public Rectangle ItemAspectArea { get; set; } = new Rectangle();
        /// <summary>
        /// Location of aspect.
        /// </summary>
        public Rectangle ItemAspectLocation { get; set; } = new Rectangle();
        /// <summary>
        /// Location of all sockets.
        /// </summary>
        public List<Rectangle> ItemSocketLocations { get; set; } = new List<Rectangle>();
        /// <summary>
        /// Location of all splitters.
        /// </summary>
        public List<Rectangle> ItemSplitterLocations { get; set; } = new List<Rectangle>();
        public int Offset { get; set; } = 0;
        public List<OcrResultDescriptor> OcrResultAffixes { get; set; } = new();
        public OcrResult OcrResultAspect { get; set; } = new();
        public Dictionary<string, int> PerformanceResults { get; set; } = new Dictionary<string, int>
        {
            { "total", 0 },
            { "Tooltip", 0},
            { "AffixLocations", 0},
            { "AspectLocations", 0},
            { "SocketLocations", 0},
            { "SplitterLocations", 0},
            { "AffixAreas", 0},
            { "AspectAreas", 0},
            { "Affixes", 0},
            { "Aspects", 0}
        };
    }
}
