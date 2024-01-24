using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class OcrDebugInfo
    {
        public int AreaIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public string TextClean { get; set; } = string.Empty;
        public string AffixId { get; set; } = string.Empty;
        public string AffixDescription { get; set; } = string.Empty;
        public string Scorer { get; set; } = string.Empty;
        public string ScorerResult { get; set; } = string.Empty;
    }
}
