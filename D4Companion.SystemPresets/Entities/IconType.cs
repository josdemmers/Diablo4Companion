using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.SystemPresets.Entities
{
    public class IconType
    {
        public int Count { get; set; } = 0;
        public string DisplayName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SelectedScreenshot { get; set; } = string.Empty;
        public int PositionX { get; set; } = 0;
        public int PositionY { get; set; } = 0;
        public int Width { get; set; } = 5;
        public int Height { get; set; } = 5;
    }
}
