using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IAffixManager
    {
        List<AffixInfo> Affixes
        {
            get;
        }

        List<AspectInfo> Aspects
        {
            get;
        }
    }
}
