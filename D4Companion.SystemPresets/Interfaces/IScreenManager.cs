using D4Companion.SystemPresets.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.SystemPresets.Interfaces
{
    public interface IScreenManager
    {
        List<ScreenCapture> ScreenCaptures { get; }
    }
}
