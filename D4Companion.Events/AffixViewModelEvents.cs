using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class ToggleOverlayFromGUIEvent : PubSubEvent<ToggleOverlayFromGUIEventParams>
    {
    }

    public class ToggleOverlayFromGUIEventParams
    {
        public bool IsEnabled { get; set; } = false;
    }
}
