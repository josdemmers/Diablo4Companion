using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class RuneInfo
    {
        public string IdSno { get; set; } = string.Empty;
        public string IdName { get; set; } = string.Empty;
        public string RuneType { get; set; } = string.Empty;

        // Localisation
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionClean { get; set; } = string.Empty;
        public string RuneDescription { get; set; } = string.Empty;
        public string RuneOverflowBehavior { get; set; } = string.Empty;
    }
}
