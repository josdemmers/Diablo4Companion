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
        public OcrResultAffix OcrResult { get; set; } = new OcrResultAffix();
    }

    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public string TextClean { get; set; } = string.Empty;
    }

    public class OcrResultAffix : OcrResult
    {
        public string AffixId { get; set; } = string.Empty;
    }

    public class OcrResultItemType : OcrResult
    {
        public int Similarity {  get; set; } = 0;
        public string Type { get; set; } = string.Empty;
        public string TypeId { get; set; } = string.Empty;
    }
}
