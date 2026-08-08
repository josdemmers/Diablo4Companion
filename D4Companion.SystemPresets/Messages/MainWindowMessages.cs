using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.SystemPresets.Messages
{
    public class ActiveScreenChangedMessage(ActiveScreenChangedMessageParams activeScreenChangedMessageParams) : ValueChangedMessage<ActiveScreenChangedMessageParams>(activeScreenChangedMessageParams)
    {
    }

    public class ActiveScreenChangedMessageParams
    {
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }

    public class ApplicationLoadedMessage
    {

    }

    public class DuplicatorsCreatedMessage
    {

    }

    public class ScreenAddedMessage
    {

    }

    public class ScreenUpdatedMessage
    {

    }
}
