using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Entities
{
    public class ScreenCapture
    {
        public BitmapSource? BitmapSource { get; set; } = null;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
