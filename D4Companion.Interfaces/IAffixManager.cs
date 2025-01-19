using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixManager
    {
        List<AffixInfo> Affixes { get; }
        List<AffixPreset> AffixPresets { get; }
        List<AspectInfo> Aspects { get; }
        List<SigilInfo> Sigils { get; }
        List<UniqueInfo> Uniques { get; }
        List<RuneInfo> Runes { get; }

        void AddAffix(AffixInfo affixInfo, string itemType);
        void AddAffixPreset(AffixPreset affixPreset);
        void AddAspect(AspectInfo aspectInfo, string itemType);
        void AddSigil(SigilInfo sigilInfo, string itemType);
        void AddUnique(UniqueInfo uniqueInfo);
        void AddRune(RuneInfo runeInfo);
        ItemAffix GetAffix(string affixId, string affixType, string itemType);
        string GetAffixDescription(string affixId);
        string GetAffixId(string affixSno);
        AffixInfo? GetAffixInfoMaxrollByIdSno(string affixIdSno);
        AffixInfo? GetAffixInfoMaxrollByIdName(string affixIdName);
        double GetAffixMinimalValue(string idName);
        ItemAffix GetAspect(string aspectId, string itemType);
        string GetAspectDescription(string aspectId);
        //string GetAspectId(int aspectSno);
        string GetAspectName(string aspectId);
        AspectInfo? GetAspectInfoMaxrollByIdSno(string aspectIdSno);
        AspectInfo? GetAspectInfoMaxrollByIdName(string aspectIdName);
        ItemAffix GetSigil(string affixId, string itemType);
        string GetSigilDescription(string sigilId);
        string GetSigilDungeonTier(string sigilId);
        string GetSigilType(string sigilId);
        string GetSigilName(string sigilId);
        ItemAffix GetUnique(string uniqueId, string itemType);
        string GetUniqueDescription(string uniqueId);
        UniqueInfo? GetUniqueInfoByIdSno(int idSno);
        string GetUniqueName(string uniqueId);
        ItemAffix GetRune(string runeId, string itemType);
        string GetRuneDescription(string runeId);
        string GetRuneName(string runeId);
        string GetGearOrSigilAffixDescription(string value);
        bool IsDuplicate(ItemAffix itemAffix);
        void RemoveAffix(ItemAffix itemAffix);
        void RemoveAspect(ItemAffix itemAffix);
        void RemoveSigil(ItemAffix itemAffix);
        void RemoveUnique(ItemAffix itemAffix);
        void RemoveRune(ItemAffix itemAffix);
        void RemoveAffixPreset(AffixPreset affixPreset);
        void RenamePreset(string oldName, string newName);
        void ResetMinimalAffixValues();
        void SaveAffixColor(ItemAffix itemAffix);
        void SaveAffixPresets();
        void SetAffixMinimalValue(string idName, double minimalValue);
        void SetSigilDungeonTier(SigilInfo sigilInfo, string tier);
        void SetIsAnyType(ItemAffix itemAffix, bool isAnyType);
        
    }
}
