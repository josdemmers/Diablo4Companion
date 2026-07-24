using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class InfinityBuildsBuild
    {
        public string Date { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public List<InfinityBuildsBuildVariant> Variants { get; set; } = new();
    }

    public class InfinityBuildsBuildVariant
    {
        public string Name { get; set; } = string.Empty;
        public AffixPreset AffixPreset { get; set; } = new();

        public List<InfinityBuildsAffix> Helm { get; set; } = new();
        public List<InfinityBuildsAffix> Chest { get; set; } = new();
        public List<InfinityBuildsAffix> Gloves { get; set; } = new();
        public List<InfinityBuildsAffix> Pants { get; set; } = new();
        public List<InfinityBuildsAffix> Boots { get; set; } = new();
        public List<InfinityBuildsAffix> Amulet { get; set; } = new();
        public List<InfinityBuildsAffix> Ring { get; set; } = new();
        public List<InfinityBuildsAffix> Weapon { get; set; } = new();
        public List<InfinityBuildsAffix> Ranged { get; set; } = new();
        public List<InfinityBuildsAffix> Offhand { get; set; } = new();
        public List<string> Aspect { get; set; } = new();
        public List<string> Uniques { get; set; } = new();
        public List<string> Runes { get; set; } = new();

        public List<ParagonBoard> ParagonBoards { get; set; } = new();
    }

    public class InfinityBuildsAffix
    {
        public string AffixText { get; set; } = string.Empty;
        public List<string> AffixTextList { get; set; } = new();
        public bool IsGreater { get; set; } = false;
        public bool IsImplicit { get; set; } = false;
        public bool IsTempered { get; set; } = false;
    }
}
