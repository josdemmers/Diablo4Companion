using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.SystemPresets.Entities
{
    public class SystemPreset
    {
        public List<IconType> IconTypes { get; set; } = new List<IconType>();
        public string Name { get; set; } = string.Empty;        
    }
}
