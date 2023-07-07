using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
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