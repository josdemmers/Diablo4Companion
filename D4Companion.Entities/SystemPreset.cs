using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class SystemPreset
    {
        public string FileName { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public string Config { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string TooltipWidth { get; set; } = string.Empty;
        public string BrightnessSliders { get; set; } = string.Empty;
    }
}