using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AffixPresetV2
    {
        public string Name { get; set; } = string.Empty;
        public List<ItemAffixV2> ItemAffixes { get; set; } = new List<ItemAffixV2>();
        public List<ItemAffixV2> ItemAspects { get; set; } = new List<ItemAffixV2>();
    }
}