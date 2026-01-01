using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsProfile
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Url
        { 
            get
            {
                return $"https://mobalytics.gg/diablo-4/profile/{Name.ToLower()}";
            }
        }

        public List<MobalyticsProfileBuildVariant> Variants { get; set; } = new();
    }

    public class MobalyticsProfileBuildVariant
    {
        public string Date { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
