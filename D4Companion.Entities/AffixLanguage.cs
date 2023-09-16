using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class AffixLanguage
    {
        public AffixLanguage()
        {
            
        }
        public AffixLanguage(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
