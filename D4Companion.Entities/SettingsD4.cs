
using System.Windows.Input;

namespace D4Companion.Entities
{
    public class SettingsD4
    {
        public int AffixAreaHeightOffsetTop { get; set; } = 10;
        public int AffixAreaHeightOffsetBottom { get; set; } = 10;
        public int AffixAspectAreaWidthOffset { get; set; } = 18;
        public int AspectAreaHeightOffsetTop { get; set; } = 10;
        public bool AspectCounter { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool ControllerMode { get; set; } = false;
        public bool DebugMode { get; set; } = false;
        public bool DevMode { get; set; } = false;
        public bool ExperimentalModeConsumables { get; set; } = false;
        public bool ExperimentalModeSeasonal { get; set; } = false;
        public bool IsTopMost { get; set; } = false;
        public int OverlayFontSize { get; set; } = 18;
        public int OverlayIconPosX { get; set; } = 0;
        public int OverlayIconPosY { get; set; } = 0;
        public string SelectedAffixLanguage { get; set; } = "enUS";
        public string SelectedAppLanguage { get; set; } = "en-US";
        public string SelectedAffixPreset { get; set; } = string.Empty;
        public string SelectedSystemPreset { get; set; } = "1440p_SMF_en";
        public int ThresholdMin { get; set; } = 60;
        public int ThresholdMax { get; set; } = 255;
        public double ThresholdSimilarityTooltip { get; set; } = 0.05;
        public double ThresholdSimilarityType { get; set; } = 0.05;
        public double ThresholdSimilarityAffixLocation { get; set; } = 0.05;
        public double ThresholdSimilarityAffix { get; set; } = 0.05;
        public double ThresholdSimilarityAspectLocation { get; set; } = 0.05;
        public double ThresholdSimilarityAspect { get; set; } = 0.05;
        public double ThresholdSimilaritySocketLocation { get; set; } = 0.05;
        public double ThresholdSimilaritySplitterLocation { get; set; } = 0.05;
        public int TooltipWidth { get; set; } = 500;
        public string SelectedOverlayMarkerMode { get; set; } = "Show All";
        public string SelectedSigilDisplayMode { get; set; } = "Whitelisting";

        public KeyBindingConfig KeyBindingConfigSwitchPreset { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Switch Preset",
            KeyGestureKey = Key.F5,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigToggleOverlay { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Toggle Overlay",
            KeyGestureKey = Key.F12,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigAspectCounterIncrease { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Aspect Counter (+)",
            KeyGestureKey = Key.OemPlus,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigAspectCounterDecrease { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Aspect Counter (-)",
            KeyGestureKey = Key.OemMinus,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigAspectCounterReset { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Aspect Counter (reset)",
            KeyGestureKey = Key.D0,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigToggleDebugLockScreencapture { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Debug: Toggle Lock Screencapture",
            KeyGestureKey = Key.F11,
            KeyGestureModifier = ModifierKeys.Control
        };

    }
}
