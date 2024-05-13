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
        ItemAffix GetAffix(string affixId, string itemType);
        string GetAffixDescription(string affixId);
        string GetAffixId(int affixSno);
        AffixInfo? GetAffixInfo(AffixInfo affixInfo);
        AffixInfo? GetAffixInfoFromFull(int affixSno);
        ItemAffix GetAspect(string aspectId, string itemType);
        string GetAspectDescription(string aspectId);
        string GetAspectId(int aspectSno);
        string GetAspectName(string aspectId);
        ItemAffix GetSigil(string affixId, string itemType);
        string GetSigilDescription(string sigilId);
        string GetSigilDungeonTier(string sigilId);
        string GetSigilType(string sigilId);
        string GetSigilName(string sigilId);
        string GetGearOrSigilAffixDescription(string value);
        bool IsAffixSelected(AffixInfo affixInfo, string itemType);
        void RemoveAffix(AffixInfo affixInfo, string itemType);
        void RemoveAffix(ItemAffix itemAffix);
        void RemoveAspect(ItemAffix itemAffix);
        void RemoveSigil(ItemAffix itemAffix);
        void RemoveAffixPreset(AffixPreset affixPreset);
        void SaveAffixColor(ItemAffix itemAffix);
        void SetSigilDungeonTier(SigilInfo sigilInfo, string tier);
    }
}
