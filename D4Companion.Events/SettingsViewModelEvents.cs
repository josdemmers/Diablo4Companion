using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class SystemPresetChangedEvent : PubSubEvent
    {
    }

    public class ReloadAffixesGuiRequestEvent : PubSubEvent
    { 
    }

    public class UpdateHotkeysRequestEvent : PubSubEvent
    { 
    }
}
