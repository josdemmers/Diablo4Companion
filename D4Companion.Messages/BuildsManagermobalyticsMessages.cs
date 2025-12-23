using CommunityToolkit.Mvvm.Messaging.Messages;
using D4Companion.Entities;

namespace D4Companion.Messages
{
    public class MobalyticsCompletedMessage
    {

    }

    public class MobalyticsStatusUpdateMessage(MobalyticsStatusUpdateMessageParams mobalyticsStatusUpdateMessageParams) : ValueChangedMessage<MobalyticsStatusUpdateMessageParams>(mobalyticsStatusUpdateMessageParams)
    {

    }
    
    public class MobalyticsStatusUpdateMessageParams
    {
        public MobalyticsBuild Build { get; set; } = new();
        public MobalyticsProfile Profile { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class MobalyticsBuildsLoadedMessage
    {

    }

    public class MobalyticsProfilesLoadedMessage
    {

    }
}
