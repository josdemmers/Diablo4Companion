﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class SystemPresetDefaults
    {
        public int AffixAreaHeightOffsetTop { get; set; }
        public int AffixAreaHeightOffsetBottom { get; set; }
        public int AffixAspectAreaWidthOffset { get; set; }
        public int AspectAreaHeightOffsetTop { get; set; }
        public int ParagonLeftOffsetCollapsed { get; set; } = -200;
        public int ParagonNodeSize { get; set; } = 40;
        public int ParagonNodeSizeCollapsed { get; set; } = 23;
        public int ParagonTopOffsetCollapsed { get; set; } = -50;
        public int ThresholdMin { get; set; }
        public int ThresholdMax { get; set; }
        public int TooltipHeightType { get; set; }
        public int TooltipWidth { get; set; }
    }
}