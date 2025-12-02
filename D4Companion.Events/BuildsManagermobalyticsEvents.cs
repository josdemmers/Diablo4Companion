using D4Companion.Entities;
using Prism.Events;

namespace D4Companion.Events
{
    public class MobalyticsCompletedEvent : PubSubEvent
    {

    }

    public class MobalyticsStatusUpdateEvent : PubSubEvent<MobalyticsStatusUpdateEventParams>
    {

    }
    
    public class MobalyticsStatusUpdateEventParams
    {
        public MobalyticsBuild Build { get; set; } = new();
        public MobalyticsProfile Profile { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class MobalyticsBuildsLoadedEvent : PubSubEvent
    {

    }

    public class MobalyticsProfilesLoadedEvent : PubSubEvent
    {

    }
}
