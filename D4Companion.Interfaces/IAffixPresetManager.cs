using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixPresetManager
    {
        List<AffixPreset> AffixPresets { get; }
        List<ItemAffix> ItemAffixes { get; }
        List<ItemType> ItemTypes { get; }

        void AddAffixPreset(AffixPreset affixPreset);
        void RemoveAffixPreset(AffixPreset affixPreset);
        void SaveAffixPresets();
    }
}