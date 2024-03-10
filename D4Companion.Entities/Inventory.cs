using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class Inventory
    {
        public Dictionary<string, int> Aspects { get; set; } = new();
    }
}
