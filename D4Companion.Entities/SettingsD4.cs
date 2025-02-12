
using System.Windows.Input;
using System.Windows.Media;

namespace D4Companion.Entities
{
    public class SettingsD4
    {
        public int AffixAreaHeightOffsetTop { get; set; } = 10;
        public int AffixAreaHeightOffsetBottom { get; set; } = 10;
        public int AffixAspectAreaWidthOffset { get; set; } = 18;
        public int AspectAreaHeightOffsetTop { get; set; } = 10;
        public bool CheckForUpdates { get; set; } = true;
        public bool ControllerMode { get; set; } = false;
        public bool DebugMode { get; set; } = false;
        public Color DefaultColorGreater { get; set; } = Colors.Green;
        public Color DefaultColorImplicit { get; set; } = Colors.Green;
        public Color DefaultColorNormal { get; set; } = Colors.Green;
        public Color DefaultColorTempered { get; set; } = Colors.Green;
        public Color DefaultColorAspects { get; set; } = Colors.Green;
        public Color DefaultColorUniques { get; set; } = Colors.Green;
        public Color DefaultColorRunes { get; set; } = Colors.Green;
        public bool DevMode { get; set; } = false;
        public bool DungeonTiers { get; set; } = true;
        public bool IsAspectDetectionEnabled { get; set; } = true;
        public bool IsDebugInfoEnabled { get; set; } = false;
        public bool IsImportParagonD4BuildsEnabled { get; set; } = true;
        public bool IsImportUniqueAffixesD4BuildsEnabled { get; set; } = false;
        public bool IsImportUniqueAffixesMaxrollEnabled { get; set; } = false;
        public bool IsImportUniqueAffixesMobalyticsEnabled { get; set; } = false;
        public bool IsItemPowerLimitEnabled { get; set; } = false;
        public bool IsMinimalAffixValueFilterEnabled { get; set; } = false;
        public bool IsMultiBuildModeEnabled { get; set; } = false;
        public bool IsParagonModeActive { get; set; } = false;
        public bool IsRuneDetectionEnabled { get; set; } = true;
        public bool IsTemperedAffixDetectionEnabled { get; set; } = true;
        public bool IsToggleCoreActive { get; set; } = true;
        public bool IsToggleBarbarianActive { get; set; } = true;
        public bool IsToggleDruidActive { get; set; } = true;
        public bool IsToggleNecromancerActive { get; set; } = true;
        public bool IsToggleRogueActive { get; set; } = true;
        public bool IsToggleSorcererActive { get; set; } = true;
        public bool IsToggleSpiritbornActive { get; set; } = true;
        public bool IsToggleDungeonsActive { get; set; } = true;
        public bool IsTogglePositiveActive { get; set; } = true;
        public bool IsToggleMinorActive { get; set; } = true;
        public bool IsToggleMajorActive { get; set; } = true;
        public bool IsToggleRuneConditionActive { get; set; } = true;
        public bool IsToggleRuneEffectActive { get; set; } = true;
        public bool IsTopMost { get; set; } = false;
        public bool IsTradeOverlayEnabled { get; set; } = true;
        public bool IsUniqueDetectionEnabled { get; set; } = true;
        public int ItemPowerLimit { get; set; } = 800;
        public bool LaunchMinimized { get; set; } = false;
        public int MinimalOcrMatchType { get; set; } = 80;
        public bool MinimizeToTray { get; set; } = false;
        public List<MultiBuild> MultiBuildList { get; set; } = new List<MultiBuild>();
        public int OverlayFontSize { get; set; } = 18;
        public int OverlayIconPosX { get; set; } = 0;
        public int OverlayIconPosY { get; set; } = 0;
        public int OverlayUpdateDelay { get; set; } = 5;
        public int ParagonNodeSize { get; set; } = 40;
        public int ScanHeight { get; set; } = 50;
        public int ScreenCaptureDelay { get; set; } = 50;
        public string SelectedAffixLanguage { get; set; } = "enUS";
        public string SelectedAppLanguage { get; set; } = "en-US";
        public string SelectedAffixPreset { get; set; } = string.Empty;
        public string SelectedSystemPreset { get; set; } = "1440p_SMF_en";
        public bool ShowCurrentItem { get; set; } = true;
        public bool ShowOverlayIcon { get; set; } = false;
        public int ThresholdMin { get; set; } = 70;
        public int ThresholdMax { get; set; } = 255;
        public double ThresholdSimilarityTooltip { get; set; } = 0.05;
        public double ThresholdSimilarityAffixLocation { get; set; } = 0.05;
        public double ThresholdSimilarityAspectLocation { get; set; } = 0.05;
        public double ThresholdSimilaritySocketLocation { get; set; } = 0.05;
        public double ThresholdSimilaritySplitterLocation { get; set; } = 0.05;
        public int TooltipMaxHeight { get; set; } = 200;
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

        public KeyBindingConfig KeyBindingConfigSwitchOverlay { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Switch Overlay",
            KeyGestureKey = Key.F6,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigTakeScreenshot { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Take Screenshot",
            KeyGestureKey = Key.F10,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigToggleController { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Toggle Controller",
            KeyGestureKey = Key.F9,
            KeyGestureModifier = ModifierKeys.Control
        };

        public KeyBindingConfig KeyBindingConfigToggleOverlay { get; set; } = new KeyBindingConfig
        {
            IsEnabled = false,
            Name = "Toggle Overlay",
            KeyGestureKey = Key.F12,
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
