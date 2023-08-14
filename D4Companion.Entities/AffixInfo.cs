using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AffixInfo
    {
        public int IdSno { get; set; }
        public string IdName { get; set; } = string.Empty;
        public List<int> AllowedForPlayerClass { get; set; } = new List<int>();
        public List<int> AllowedItemLabels { get; set; } = new List<int>();
        public string Description { get; set; } = string.Empty;
        public string LocalisationId { get; set; } = string.Empty;
        public string Localisation { get; set; } = string.Empty;
        /// <summary>
        /// None: 0 (Affixes)
        /// Legendary: 1 (Aspects)
        /// Unique: 2
        /// Test: 3
        /// </summary>
        public int MagicType { get; set; }
    }
}
