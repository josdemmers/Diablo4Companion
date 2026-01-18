using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class D2CoreBuild
    {
        public D2CoreBuildDataJson Data { get; set; } = new();

        public string Date { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
