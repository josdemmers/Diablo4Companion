using D4Companion.Entities;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Events
{
    public class ScreenProcessItemTooltipReadyEvent : PubSubEvent<ScreenProcessItemTooltipReadyEventParams>
    {
    }

    public class ScreenProcessItemTooltipReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemTypeReadyEvent : PubSubEvent<ScreenProcessItemTypeReadyEventParams>
    {
    }

    public class ScreenProcessItemTypeReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixLocationsReadyEvent : PubSubEvent<ScreenProcessItemAffixLocationsReadyEventParams>
    {
    }

    public class ScreenProcessItemAffixLocationsReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixAreasReadyEvent : PubSubEvent<ScreenProcessItemAffixAreasReadyEventParams>
    {
    }

    public class ScreenProcessItemAffixAreasReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixesReadyEvent : PubSubEvent<ScreenProcessItemAffixesReadyEventParams>
    {
    }

    public class ScreenProcessItemAffixesReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAspectLocationReadyEvent : PubSubEvent<ScreenProcessItemAspectLocationReadyEventParams>
    {
    }

    public class ScreenProcessItemAspectLocationReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAspectReadyEvent : PubSubEvent<ScreenProcessItemAspectReadyEventParams>
    {
    }

    public class ScreenProcessItemAspectReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class TooltipDataReadyEvent : PubSubEvent<TooltipDataReadyEventParams>
    {
    }

    public class TooltipDataReadyEventParams
    {
        public ItemTooltipDescriptor Tooltip { get; set; } = new ItemTooltipDescriptor();
    }
}
