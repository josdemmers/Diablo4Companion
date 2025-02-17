using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ParagonBoard
    {
        public string Name { get; set; } = string.Empty;
        public string Glyph { get; set; } = string.Empty;
        public string Rotation { get; set; } = string.Empty;
        public bool[] Nodes { get; set; } = new bool[21*21];
    }
}
