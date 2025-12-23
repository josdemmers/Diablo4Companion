using CommunityToolkit.Mvvm.Messaging.Messages;

namespace D4Companion.Messages
{

    public class DownloadProgressUpdatedMessage(HttpProgress progress) : ValueChangedMessage<HttpProgress>(progress) { }
    public class UploadProgressUpdatedMessage(HttpProgress progress) : ValueChangedMessage<HttpProgress>(progress) { }
    public class DownloadCompletedMessage(string message) : ValueChangedMessage<string>(message) { }
    public class DownloadSystemPresetCompletedMessage(string message) : ValueChangedMessage<string>(message) { }

    public class HttpProgress
    {
        public long Bytes { get; set; }
        public int Progress { get; set; }
    }
}
