using System.Drawing;

namespace D4Companion.Interfaces
{
    public interface IOcrHandler
    {
        string ConvertToAffix(Image image);
        string ConvertToAspect(Image image);
        string ConvertToSigil(Image image);
    }
}
