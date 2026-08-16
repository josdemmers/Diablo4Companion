using D4Companion.SystemPresets.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Interfaces
{
    public interface IScreenManager
    {
        List<ScreenCapture> ScreenCaptures { get; }

        void SaveBitmapSourceToFile(BitmapSource bitmap, string filePath);
    }
}
