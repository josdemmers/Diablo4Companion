using System.Windows.Media;

namespace D4Companion.Entities
{
    public class ItemAffix
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.Green;
        public bool IsGreater { get; set; } = false;
        public bool IsImplicit { get; set; } = false;
        public bool IsTempered { get; set; } = false;
    }
}
