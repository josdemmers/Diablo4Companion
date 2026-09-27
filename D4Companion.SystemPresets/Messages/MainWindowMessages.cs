using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.SystemPresets.Messages
{
    public class ApplicationClosingMessage
    {

    }

    public class ApplicationLoadedMessage
    {

    }

    public class CursorUpdatedMessage(CursorUpdatedMessageParams cursorUpdatedMessageParams) : ValueChangedMessage<CursorUpdatedMessageParams>(cursorUpdatedMessageParams)
    {
    }

    public class CursorUpdatedMessageParams
    {     
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
    }

    public class DuplicatorsCreatedMessage
    {

    }

    public class IconTypeROIUpdatedMessage
    {

    }

    public class ScreenAddedMessage
    {

    }

    public class ScreenUpdatedMessage
    {

    }

    public class SystemPresetsUpdatedMessage
    {

    }

    public class TakeScreenshotMessage
    {

    }

    public class UpdateScreenshotMessage
    {

    }
}
