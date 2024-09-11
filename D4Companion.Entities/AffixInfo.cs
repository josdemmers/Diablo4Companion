using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AffixInfo
    {
        public string IdSno { get; set; } = string.Empty;
        public string IdName { get; set; } = string.Empty;

        public List<string> IdSnoList { get; set; } = new List<string>();
        public List<string> IdNameList { get; set; } = new List<string>();

        public int AffixType { get; set; }
        public int Category { get; set; }
        public int Flags { get; set; }
        public bool IsTemperingAvailable { get; set; } = false;

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
        public List<AffixAttribute> AffixAttributes { get; set; } = new List<AffixAttribute>();

        // Localisation
        public string ClassRestriction { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionClean { get; set; } = string.Empty;
    }

    public class AffixAttribute
    {
        public string LocalisationId { get; set; } = string.Empty;
        public uint LocalisationParameter { get; set; } // Keep this at uint, need to automatic fix overflowed values.
        public string LocalisationAttributeFormulaValue { get; set; } = string.Empty;

        // Localisation
        public string Localisation { get; set; } = string.Empty;
    }
}
