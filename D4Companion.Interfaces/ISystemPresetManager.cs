using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface ISystemPresetManager
    {
        List<SystemPreset> SystemPresets { get; }
        List<string> AffixImages { get; }
        List<string> AspectImages { get; }
        List<AffixMapping> AffixMappings { get; }

        void AddMapping(string idName, string folder, string fileName);
        void DownloadSystemPreset(string fileName);
        void ExtractSystemPreset(string fileName);
        int GetImageUsageCount(string folder, string fileName);
        bool IsItemTypeImageFound(string itemType);
        void RemoveMapping(string idName, string folder, string fileName);
    }
}
