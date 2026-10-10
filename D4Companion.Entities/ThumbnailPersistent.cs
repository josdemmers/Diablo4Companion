using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.Entities
{
    public class ThumbnailPersistent
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int DestinationPositionX { get; set; } = 0;
        public int DestinationPositionY { get; set; } = 0;
        public int DestinationWidth { get; set; } = 50;
        public int DestinationHeight { get; set; } = 50;
        public int SourcePositionX { get; set; } = 0;
        public int SourcePositionY { get; set; } = 0;
        public int SourceWidth { get; set; } = 50;
        public int SourceHeight { get; set; } = 50;
    }
}
