using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixManager
    {
        List<AffixInfo> Affixes { get; }
        List<AffixPresetV2> AffixPresets { get; }
        List<AspectInfo> Aspects { get; }

        void AddAffix(AffixInfo affixInfo, string itemType);
        void AddAffixPreset(AffixPresetV2 affixPreset);
        void AddAspect(AspectInfo affixInfo, string itemType);
        string GetAffixDescription(string affixId);
        string GetAspectDescription(string aspectId);
        string GetAspectName(string aspectId);
        bool IsAffixSelected(AffixInfo affixInfo, string itemType);
        void RemoveAffix(AffixInfo affixInfo, string itemType);
        void RemoveAffix(ItemAffixV2 itemAffix);
        void RemoveAspect(ItemAffixV2 itemAffix);
        void RemoveAffixPreset(AffixPresetV2 affixPreset);
        void SaveAffixColor(ItemAffixV2 itemAffix);
    }
}
