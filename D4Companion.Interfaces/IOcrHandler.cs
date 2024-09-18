using D4Companion.Entities;
using System.Drawing;

namespace D4Companion.Interfaces
{
    public interface IOcrHandler
    {
        OcrResultAffix ConvertToAffix(string rawText);
        OcrResultAffix ConvertToAspect(string rawText);
        OcrResultAffix ConvertToUnique(string rawText);
        OcrResultAffix ConvertToRune(string rawText);
        OcrResultAffix ConvertToSigil(string rawText);
        OcrResultItemType ConvertToItemType(string rawText);
        OcrResult ConvertToPower(string rawText);
        string ConvertToText(Image image);
        string ConvertToTextUpperTooltipSection(Image image);
    }
}
