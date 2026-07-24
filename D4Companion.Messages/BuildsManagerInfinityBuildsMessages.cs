using CommunityToolkit.Mvvm.Messaging.Messages;
using D4Companion.Entities;

namespace D4Companion.Messages
{
    public class InfinityBuildsCompletedMessage
    {

    }

    public class InfinityBuildsStatusUpdateMessage(InfinityBuildsStatusUpdateMessageParams infinityBuildsStatusUpdateMessageParams) : ValueChangedMessage<InfinityBuildsStatusUpdateMessageParams>(infinityBuildsStatusUpdateMessageParams)
    {

    }
    
    public class InfinityBuildsStatusUpdateMessageParams
    {
        public InfinityBuildsBuild Build { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class InfinityBuildsBuildsLoadedMessage
    {

    }
}
