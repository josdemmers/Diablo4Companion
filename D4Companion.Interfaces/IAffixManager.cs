using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixManager
    {
        List<AffixInfo> Affixes { get; }
        List<AffixPreset> AffixPresets { get; }
        List<AspectInfo> Aspects { get; }
        List<SigilInfo> Sigils { get; }

        void AddAffix(AffixInfo affixInfo, string itemType);
        void AddAffixPreset(AffixPreset affixPreset);
        void AddAspect(AspectInfo aspectInfo, string itemType);
        void AddSigil(SigilInfo sigilInfo, string itemType);
        string GetAffixDescription(string affixId);
        string GetAspectDescription(string aspectId);
        string GetAspectName(string aspectId);
        string GetSigilDescription(string sigilId);
        string GetSigilName(string sigilId);
        bool IsAffixSelected(AffixInfo affixInfo, string itemType);
        void RemoveAffix(AffixInfo affixInfo, string itemType);
        void RemoveAffix(ItemAffix itemAffix);
        void RemoveAspect(ItemAffix itemAffix);
        void RemoveSigil(ItemAffix itemAffix);
        void RemoveAffixPreset(AffixPreset affixPreset);
        void SaveAffixColor(ItemAffix itemAffix);
    }
}
