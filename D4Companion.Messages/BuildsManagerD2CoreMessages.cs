using CommunityToolkit.Mvvm.Messaging.Messages;
using D4Companion.Entities;

namespace D4Companion.Messages
{
    public class D2CoreCompletedMessage
    {

    }

    public class D2CoreStatusUpdateMessage(D2CoreStatusUpdateMessageParams d2CoreStatusUpdateMessageParams) : ValueChangedMessage<D2CoreStatusUpdateMessageParams>(d2CoreStatusUpdateMessageParams)
    {

    }
    
    public class D2CoreStatusUpdateMessageParams
    {
        public D2CoreBuild Build { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class D2CoreBuildsLoadedMessage
    {

    }
}
