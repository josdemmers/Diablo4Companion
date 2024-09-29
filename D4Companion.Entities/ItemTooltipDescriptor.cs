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
        public int ItemPower { get; set; }
        public Rectangle Location { get; set; } = Rectangle.Empty;
        public bool HasTooltipTopSplitter { get; set; } = false;
        public bool IsUniqueItem { get; set; } = false;
        /// <summary>
        /// List of detected affixes.
        /// </summary>
        public List<Tuple<int,ItemAffix>> ItemAffixes { get; set; } = new List<Tuple<int, ItemAffix>>();
        public List<Tuple<int, ItemAffix>> ItemAffixesBuild1 { get; set; } = new List<Tuple<int, ItemAffix>>();
        public List<Tuple<int, ItemAffix>> ItemAffixesBuild2 { get; set; } = new List<Tuple<int, ItemAffix>>();
        public List<Tuple<int, ItemAffix>> ItemAffixesBuild3 { get; set; } = new List<Tuple<int, ItemAffix>>();
        /// <summary>
        /// Areas containing an affix.
        /// </summary>
        public List<ItemAffixAreaDescriptor> ItemAffixAreas { get; set; } = new List<ItemAffixAreaDescriptor>();
        /// <summary>
        /// Location of all affixes.
        /// </summary>
        public List<ItemAffixLocationDescriptor> ItemAffixLocations { get; set; } = new List<ItemAffixLocationDescriptor>();
        /// <summary>
        /// Detected aspect.
        /// </summary>
        public ItemAffix ItemAspect { get; set; } = new ItemAffix();
        public ItemAffix ItemAspectBuild1 { get; set; } = new ItemAffix();
        public ItemAffix ItemAspectBuild2 { get; set; } = new ItemAffix();
        public ItemAffix ItemAspectBuild3 { get; set; } = new ItemAffix();
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
        public List<ItemSplitterLocationDescriptor> ItemSplitterLocations { get; set; } = new List<ItemSplitterLocationDescriptor>();
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;
        public List<OcrResultDescriptor> OcrResultAffixes { get; set; } = new();
        public OcrResultAffix OcrResultAspect { get; set; } = new();
        public OcrResultItemType OcrResultItemType { get; set; } = new();
        public OcrResult OcrResultPower { get; set; } = new();
        public Dictionary<string, int> PerformanceResults { get; set; } = new Dictionary<string, int>
        {
            { "total", 0 },
            { "Tooltip", 0},
            { "ItemTypePower", 0},
            { "AffixLocations", 0},
            { "AspectLocations", 0},
            { "SocketLocations", 0},
            { "SplitterLocations", 0},
            { "AffixAreas", 0},
            { "AspectAreas", 0},
            { "Affixes", 0},
            { "Aspects", 0}
        };
        public TradeItem? TradeItem { get; set; } = null;
    }
}
