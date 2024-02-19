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

    public class ScreenProcessItemAffixesOcrReadyEvent : PubSubEvent<ScreenProcessItemAffixesOcrReadyEventParams>
    {
    }

    public class ScreenProcessItemAffixesOcrReadyEventParams
    {
        public List<OcrResultDescriptor> OcrResults { get; set; } = new();
    }

    public class ScreenProcessItemAspectLocationReadyEvent : PubSubEvent<ScreenProcessItemAspectLocationReadyEventParams>
    {
    }

    public class ScreenProcessItemAspectLocationReadyEventParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAspectAreaReadyEvent : PubSubEvent<ScreenProcessItemAspectAreaReadyEventParams>
    {
    }

    public class ScreenProcessItemAspectAreaReadyEventParams
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

    public class ScreenProcessItemAspectOcrReadyEvent : PubSubEvent<ScreenProcessItemAspectOcrReadyEventParams>
    {
    }

    public class ScreenProcessItemAspectOcrReadyEventParams
    {
        public OcrResult OcrResult { get; set; } = new();
    }

    public class ScreenProcessItemSocketLocationsReadyEvent : PubSubEvent<ScreenProcessItemSocketLocationsReadyEventParams>
    {
    }

    public class ScreenProcessItemSocketLocationsReadyEventParams
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
