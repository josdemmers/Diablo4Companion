using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class AffixLanguageChangedEvent : PubSubEvent
    {
    }

    public class AffixPresetChangedEvent : PubSubEvent<AffixPresetChangedEventParams>
    {
    }

    public class AffixPresetChangedEventParams
    {
        public string PresetName { get; set; } = string.Empty;
    }

    public class ToggleOverlayFromGUIEvent : PubSubEvent<ToggleOverlayFromGUIEventParams>
    {
    }

    public class ToggleOverlayFromGUIEventParams
    {
        public bool IsEnabled { get; set; } = false;
    }


}
