using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MaxrollBuildDescription
    {
        public string Name { get; set; } = string.Empty;

        public string NameReadable
        {
            get => Name.Replace("-"," ");
        }

        public string Uri { get; set; } = string.Empty;
    }
}
