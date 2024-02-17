using D4Companion.Entities;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class D4BuildsCompletedEvent : PubSubEvent
    {

    }

    public class D4BuildsStatusUpdateEvent : PubSubEvent<D4BuildsStatusUpdateEventParams>
    {

    }
    
    public class D4BuildsStatusUpdateEventParams
    {
        public D4BuildsBuild Build { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class D4BuildsBuildsLoadedEvent : PubSubEvent
    {

    }
}
