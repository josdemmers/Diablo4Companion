using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AspectInfo
    {
        public string IdSno { get; set; } = string.Empty;
        public string IdName { get; set; } = string.Empty;

        public List<string> IdSnoList { get; set; } = new List<string>();
        public List<string> IdNameList { get; set; } = new List<string>();

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionClean { get; set; } = string.Empty;
        public string Localisation { get; set; } = string.Empty;
        public bool IsSeasonal { get; set; } = false;
        public bool IsCodex { get; set; } = false;
        public string Dungeon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// None: 0 (Affixes)
        /// Legendary: 1 (Aspects)
        /// Unique: 2 (Aspects)
        /// Test: 3
        /// Mythic: 4 (Aspects)
        /// </summary>
        public int MagicType { get; set; }

        /// <summary>
        /// Sorc, Druid, Barb, Rogue, Necro, Spiritborn
        /// </summary>
        public List<int> AllowedForPlayerClass { get; set; } = new List<int>();
        public List<int> AllowedItemLabels { get; set; } = new List<int>();
    }
}
