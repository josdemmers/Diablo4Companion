using D4Companion.Entities;
using System.Drawing;

namespace D4Companion.Interfaces
{
    public interface IOcrHandler
    {
        OcrResult ConvertToAffix(string rawText);
        OcrResult ConvertToAspect(string rawText);
        OcrResult ConvertToSigil(string rawText);
        string ConvertToText(Image image);
    }
}
