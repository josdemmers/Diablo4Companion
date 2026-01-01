using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.Messages
{
    public class AffixLanguageChangedMessage
    {
    }

    public class AffixPresetChangedMessage(AffixPresetChangedMessageParams affixPresetChangedMessageParams) : ValueChangedMessage<AffixPresetChangedMessageParams>(affixPresetChangedMessageParams)
    {
    }

    public class AffixPresetChangedMessageParams
    {
        public string PresetName { get; set; } = string.Empty;
    }

    public class ToggleOverlayFromGUIMessage(ToggleOverlayFromGUIMessageParams toggleOverlayFromGUIMessageParams) : ValueChangedMessage<ToggleOverlayFromGUIMessageParams>(toggleOverlayFromGUIMessageParams)
    {
    }

    public class ToggleOverlayFromGUIMessageParams
    {
        public bool IsEnabled { get; set; } = false;
    }

    /// <summary>
    /// Message is published by the following events:
    /// - Changing the controller images
    /// </summary>
    public class AvailableImagesChangedMessage
    {
    }
}
