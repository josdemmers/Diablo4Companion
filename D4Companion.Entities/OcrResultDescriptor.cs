using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class OcrResultDescriptor
    {
        public int AreaIndex { get; set; } = 0;
        public OcrResult OcrResult { get; set; } = new OcrResult();
    }

    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public string TextClean { get; set; } = string.Empty;
        public string AffixId { get; set; } = string.Empty;
    }
}
