using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    // TODO: Move this to AffixView when ItemTooltipView is deprecated.
    public class AffixPresetChangedEvent : PubSubEvent<AffixPresetChangedEventParams>
    {

    }

    public class AffixPresetChangedEventParams
    {
        public string PresetName { get; set; } = string.Empty;
    }
}
