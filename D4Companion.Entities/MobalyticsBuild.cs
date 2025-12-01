using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsBuild
    {
        public string Date { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public List<MobalyticsBuildVariant> Variants { get; set; } = new();
    }

    public class MobalyticsBuildVariant
    {
        public string Name { get; set; } = string.Empty;
        public AffixPreset AffixPreset { get; set; } = new();

        public List<MobalyticsAffix> Helm { get; set; } = new();
        public List<MobalyticsAffix> Chest { get; set; } = new();
        public List<MobalyticsAffix> Gloves { get; set; } = new();
        public List<MobalyticsAffix> Pants { get; set; } = new();
        public List<MobalyticsAffix> Boots { get; set; } = new();
        public List<MobalyticsAffix> Amulet { get; set; } = new();
        public List<MobalyticsAffix> Ring { get; set; } = new();
        public List<MobalyticsAffix> Weapon { get; set; } = new();
        public List<MobalyticsAffix> Ranged { get; set; } = new();
        public List<MobalyticsAffix> Offhand { get; set; } = new();
        public List<string> Aspect { get; set; } = new();
        public List<string> Uniques { get; set; } = new();
        public List<string> Runes { get; set; } = new();

        public List<ParagonBoard> ParagonBoards { get; set; } = new();
    }

    public class MobalyticsAffix
    {
        public string AffixText { get; set; } = string.Empty;
        public string AffixTextClean { get; set; } = string.Empty;
        public bool IsGreater { get; set; } = false;
        public bool IsImplicit { get; set; } = false;
        public bool IsTempered { get; set; } = false;
    }
}
