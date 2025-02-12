using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class D4BuildsBuild
    {
        public string Date { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public List<D4BuildsBuildVariant> Variants { get; set; } = new();
    }

    public class D4BuildsBuildVariant
    {
        public string Name { get; set; } = string.Empty;
        public AffixPreset AffixPreset {  get; set; } = new();

        public List<D4buildsAffix> Helm { get; set; } = new();
        public List<D4buildsAffix> Chest { get; set; } = new();
        public List<D4buildsAffix> Gloves { get; set; } = new();
        public List<D4buildsAffix> Pants { get; set; } = new();
        public List<D4buildsAffix> Boots { get; set; } = new();
        public List<D4buildsAffix> Amulet { get; set; } = new();
        public List<D4buildsAffix> Ring { get; set; } = new();
        public List<D4buildsAffix> Weapon { get; set; } = new();
        public List<D4buildsAffix> Ranged { get; set; } = new();
        public List<D4buildsAffix> Offhand { get; set; } = new();
        public List<string> Aspect { get; set; } = new();
        public List<string> Runes { get; set; } = new();
        public List<string> Uniques { get; set; } = new();

        public List<ParagonBoard> ParagonBoards { get; set; } = new();
    }

    public class D4buildsAffix
    {
        public string AffixText { get; set; } = string.Empty;
        public bool IsGreater { get; set; } = false;
        public bool IsImplicit { get; set; } = false;
        public bool IsTempered { get; set; } = false;
    }

    public class D4buildsParagonBoard
    {
        public List<string> Nodes { get; set; } = new();
    }
}
