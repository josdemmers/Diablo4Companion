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

        public List<string> Helm { get; set; } = new();
        public List<string> Chest { get; set; } = new();
        public List<string> Gloves { get; set; } = new();
        public List<string> Pants { get; set; } = new();
        public List<string> Boots { get; set; } = new();
        public List<string> Amulet { get; set; } = new();
        public List<string> Ring { get; set; } = new();
        public List<string> Weapon { get; set; } = new();
        public List<string> Ranged { get; set; } = new();
        public List<string> Offhand { get; set; } = new();
        public List<string> Aspect { get; set; } = new();
    }
}
