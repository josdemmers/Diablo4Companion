using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class SettingsD4
    {
        public bool DebugMode { get; set; } = false;
        public string SelectedAffixName { get; set; } = string.Empty;
        public string SelectedSystemPreset { get; set; } = "2560x1440";
        public int ThresholdMin { get; set; } = 60;
        public int ThresholdMax { get; set; } = 255;
    }
}
