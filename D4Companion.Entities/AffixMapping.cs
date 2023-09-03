using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AffixMapping
    {
        public string IdName { get; set; } = string.Empty;
        public string Folder { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
    }
}
