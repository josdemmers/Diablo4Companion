using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsBuild
    {
        public string Date { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public List<MobalyticsBuildVariant> Variants { get; set; } = new();
    }

    public class MobalyticsBuildVariant
    {
        public string Name { get; set; } = string.Empty;
    }
}
