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
        public bool DevMode { get; set; } = false;
        public bool LiteMode { get; set; } = true;
        public string SelectedAffixName { get; set; } = string.Empty;
        public string SelectedSystemPreset { get; set; } = "2560x1440_SMF_en";
        public int ThresholdMin { get; set; } = 60;
        public int ThresholdMax { get; set; } = 255;
        public double ThresholdSimilarityTooltip { get; set; } = 0.05;
        public double ThresholdSimilarityType { get; set; } = 0.05;
        public double ThresholdSimilarityAffixLocation { get; set; } = 0.05;
        public double ThresholdSimilarityAffix { get; set; } = 0.05;
        public double ThresholdSimilarityAspectLocation { get; set; } = 0.05;
        public double ThresholdSimilarityAspect { get; set; } = 0.05;
        public int TooltipWidth { get; set; } = 500;
    }
}
