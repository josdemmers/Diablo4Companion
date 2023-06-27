using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class MenuLockedEvent : PubSubEvent<MenuLockedEventParams>
    {
    }

    public class MenuLockedEventParams
    {
        public string Id { get; set; } = string.Empty;
    }

    public class MenuUnlockedEvent : PubSubEvent<MenuUnlockedEventParams>
    {
    }

    public class MenuUnlockedEventParams
    {
        public string Id { get; set; } = string.Empty;
    }

    public class ToggleOverlayEvent : PubSubEvent<ToggleOverlayEventParams> 
    {
    }
    
    public class ToggleOverlayEventParams
    {
        public bool IsEnabled { get; set; } = false;
    }
}
