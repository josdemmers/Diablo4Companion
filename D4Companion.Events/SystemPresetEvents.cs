using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class SystemPresetInfoUpdatedEvent : PubSubEvent
    {
    }

    public class SystemPresetExtractedEvent : PubSubEvent 
    {
    }

    public class SystemPresetItemTypesLoadedEvent : PubSubEvent
    { 
    }

    public class SystemPresetMappingChangedEvent : PubSubEvent
    {
    }
}
