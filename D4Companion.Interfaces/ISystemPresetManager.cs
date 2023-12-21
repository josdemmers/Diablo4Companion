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
        List<string> ControllerConfig { get; }
        List<string> ControllerImages { get; }

        void AddController(string fileName);
        void DownloadSystemPreset(string fileName);
        void ExtractSystemPreset(string fileName);
        bool IsControllerActive(string fileName);
        bool IsItemTypeImageFound(string itemType);
        void LoadControllerImages();
        void RemoveController(string fileName);
    }
}
