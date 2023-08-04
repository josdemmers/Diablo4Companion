using Prism.Events;

namespace D4Companion.Events
{
    public class InfoOccurredEvent : PubSubEvent<InfoOccurredEventParams>
    {
    }

    public class InfoOccurredEventParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class WarningOccurredEvent : PubSubEvent<WarningOccurredEventParams>
    {
    }

    public class WarningOccurredEventParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorOccurredEvent : PubSubEvent<ErrorOccurredEventParams>
    {
    }

    public class ErrorOccurredEventParams
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ExceptionOccurredEvent : PubSubEvent<ExceptionOccurredEventParams>
    {
    }

    public class ExceptionOccurredEventParams
    {
        public string Message { get; set; } = string.Empty;
    }
}