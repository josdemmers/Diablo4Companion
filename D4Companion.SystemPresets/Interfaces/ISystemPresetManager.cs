using D4Companion.SystemPresets.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Interfaces
{
    public interface ISystemPresetManager
    {
        List<SystemPreset> SystemPresets { get; }

        void AddSystemPreset(string systemPresetName);
        List<IconType> GetItemTypes();
        void RemoveSystemPreset(string systemPresetName);
        void Save(SystemPreset selectedSystemPreset);
        void SaveScreenshot(BitmapSource screenCapture, string systemPresetName);
        string UpdateScreenshot(BitmapSource screenCapture, string systemPresetName, string screenshot);
    }
}
