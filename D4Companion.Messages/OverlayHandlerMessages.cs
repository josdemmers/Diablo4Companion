using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.Messages
{
    public class MenuLockedMessage(MenuLockedMessageParams menuLockedMessageParams) : ValueChangedMessage<MenuLockedMessageParams>(menuLockedMessageParams)
    {
    }

    public class MenuLockedMessageParams
    {
        public string Id { get; set; } = string.Empty;
    }

    public class MenuUnlockedMessage(MenuUnlockedMessageParams menuUnlockedMessageParams) : ValueChangedMessage<MenuUnlockedMessageParams>(menuUnlockedMessageParams)
    {
    }

    public class MenuUnlockedMessageParams
    {
        public string Id { get; set; } = string.Empty;
    }

    public class ToggleOverlayMessage(ToggleOverlayMessageParams toggleOverlayMessageParams) : ValueChangedMessage<ToggleOverlayMessageParams>(toggleOverlayMessageParams)
    {
    }
    
    public class ToggleOverlayMessageParams
    {
        public bool IsEnabled { get; set; } = false;
    }
}
