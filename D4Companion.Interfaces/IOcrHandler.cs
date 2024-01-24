using System.Drawing;

namespace D4Companion.Interfaces
{
    public interface IOcrHandler
    {
        List<string> ConvertToAffix(Image image);
        List<string> ConvertToAspect(Image image);
        List<string> ConvertToSigil(Image image);
    }
}
