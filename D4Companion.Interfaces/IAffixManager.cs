using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixManager
    {
        List<AffixInfo> Affixes { get; }
        List<AffixPreset> AffixPresets { get; }
        List<AspectInfo> Aspects { get; }

        void AddAffix(AffixInfo affixInfo, string itemType);
        void AddAffixPreset(AffixPreset affixPreset);
        void AddAspect(AspectInfo affixInfo, string itemType);
        string GetAffixDescription(string affixId);
        string GetAspectDescription(string aspectId);
        string GetAspectName(string aspectId);
        bool IsAffixSelected(AffixInfo affixInfo, string itemType);
        void RemoveAffix(AffixInfo affixInfo, string itemType);
        void RemoveAffix(ItemAffix itemAffix);
        void RemoveAspect(ItemAffix itemAffix);
        void RemoveAffixPreset(AffixPreset affixPreset);
        void SaveAffixColor(ItemAffix itemAffix);
    }
}
