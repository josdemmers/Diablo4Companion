using CommunityToolkit.Mvvm.Messaging.Messages;
using D4Companion.Entities;

namespace D4Companion.Messages
{
    public class D4BuildsCompletedMessage
    {

    }

    public class D4BuildsStatusUpdateMessage(D4BuildsStatusUpdateMessageParams d4BuildsStatusUpdateMessageParams) : ValueChangedMessage<D4BuildsStatusUpdateMessageParams>(d4BuildsStatusUpdateMessageParams)
    {

    }
    
    public class D4BuildsStatusUpdateMessageParams
    {
        public D4BuildsBuild Build { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class D4BuildsBuildsLoadedMessage
    {

    }
}
