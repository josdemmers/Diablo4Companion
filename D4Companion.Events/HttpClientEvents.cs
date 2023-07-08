using Prism.Events;

namespace D4Companion.Events
{

    public class DownloadProgressUpdatedEvent : PubSubEvent<HttpProgress> { }
    public class UploadProgressUpdatedEvent : PubSubEvent<HttpProgress> { }
    public class DownloadCompletedEvent : PubSubEvent<string> { }

    public class HttpProgress
    {
        public long Bytes { get; set; }
        public int Progress { get; set; }
    }
}
