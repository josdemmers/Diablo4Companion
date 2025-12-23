using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.Messages
{
    public class InfoOccurredMessage(InfoOccurredMessageParams infoOccurredMessageParams) : ValueChangedMessage<InfoOccurredMessageParams>(infoOccurredMessageParams)
    {
    }

    public class InfoOccurredMessageParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class WarningOccurredMessage(WarningOccurredMessageParams warningOccurredMessageParams) : ValueChangedMessage<WarningOccurredMessageParams>(warningOccurredMessageParams)
    {
    }

    public class WarningOccurredMessageParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorOccurredMessage(ErrorOccurredMessageParams errorOccurredMessageParams) : ValueChangedMessage<ErrorOccurredMessageParams>(errorOccurredMessageParams)
    {
    }

    public class ErrorOccurredMessageParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ExceptionOccurredMessage(ExceptionOccurredMessageParams exceptionOccurredMessageParams) : ValueChangedMessage<ExceptionOccurredMessageParams>(exceptionOccurredMessageParams)
    {
    }

    public class ExceptionOccurredMessageParams
    {
        public string Message { get; set; } = string.Empty;
    }
}