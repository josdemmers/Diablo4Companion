using CommunityToolkit.Mvvm.Messaging.Messages;
using D4Companion.Entities;
using System.Drawing;

namespace D4Companion.Messages
{
    public class ScreenProcessItemTooltipReadyMessage(ScreenProcessItemTooltipReadyMessageParams screenProcessItemTooltipReadyMessageParams) : ValueChangedMessage<ScreenProcessItemTooltipReadyMessageParams>(screenProcessItemTooltipReadyMessageParams)
    {
    }

    public class ScreenProcessItemTooltipReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemTypeReadyMessage(ScreenProcessItemTypeReadyMessageParams screenProcessItemTypeReadyMessageParams) : ValueChangedMessage<ScreenProcessItemTypeReadyMessageParams>(screenProcessItemTypeReadyMessageParams)
    {
    }

    public class ScreenProcessItemTypeReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixLocationsReadyMessage(ScreenProcessItemAffixLocationsReadyMessageParams screenProcessItemAffixLocationsReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAffixLocationsReadyMessageParams>(screenProcessItemAffixLocationsReadyMessageParams)
    {
    }

    public class ScreenProcessItemAffixLocationsReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixAreasReadyMessage(ScreenProcessItemAffixAreasReadyMessageParams screenProcessItemAffixAreasReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAffixAreasReadyMessageParams>(screenProcessItemAffixAreasReadyMessageParams)
    {
    }

    public class ScreenProcessItemAffixAreasReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAffixesOcrReadyMessage(ScreenProcessItemAffixesOcrReadyMessageParams screenProcessItemAffixesOcrReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAffixesOcrReadyMessageParams>(screenProcessItemAffixesOcrReadyMessageParams)
    {
    }

    public class ScreenProcessItemAffixesOcrReadyMessageParams
    {
        public List<OcrResultDescriptor> OcrResults { get; set; } = new();
    }

    public class ScreenProcessItemAspectLocationReadyMessage(ScreenProcessItemAspectLocationReadyMessageParams screenProcessItemAspectLocationReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAspectLocationReadyMessageParams>(screenProcessItemAspectLocationReadyMessageParams)
    {
    }

    public class ScreenProcessItemAspectLocationReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAspectAreaReadyMessage(ScreenProcessItemAspectAreaReadyMessageParams screenProcessItemAspectAreaReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAspectAreaReadyMessageParams>(screenProcessItemAspectAreaReadyMessageParams)
    {
    }

    public class ScreenProcessItemAspectAreaReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemAspectOcrReadyMessage(ScreenProcessItemAspectOcrReadyMessageParams screenProcessItemAspectOcrReadyMessageParams) : ValueChangedMessage<ScreenProcessItemAspectOcrReadyMessageParams>(screenProcessItemAspectOcrReadyMessageParams)
    {
    }

    public class ScreenProcessItemAspectOcrReadyMessageParams
    {
        public OcrResultAffix OcrResult { get; set; } = new();
    }

    public class ScreenProcessItemSocketLocationsReadyMessage(ScreenProcessItemSocketLocationsReadyMessageParams screenProcessItemSocketLocationsReadyMessageParams) : ValueChangedMessage<ScreenProcessItemSocketLocationsReadyMessageParams>(screenProcessItemSocketLocationsReadyMessageParams)
    {
    }

    public class ScreenProcessItemSocketLocationsReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemSplitterLocationsReadyMessage(ScreenProcessItemSplitterLocationsReadyMessageParams screenProcessItemSplitterLocationsReadyMessageParams) : ValueChangedMessage<ScreenProcessItemSplitterLocationsReadyMessageParams>(screenProcessItemSplitterLocationsReadyMessageParams)
    {
    }

    public class ScreenProcessItemSplitterLocationsReadyMessageParams
    {
        public Bitmap? ProcessedScreen { get; set; }
    }

    public class ScreenProcessItemTypePowerOcrReadyMessage(ScreenProcessItemTypePowerOcrReadyMessageParams screenProcessItemTypePowerOcrReadyMessageParams) : ValueChangedMessage<ScreenProcessItemTypePowerOcrReadyMessageParams>(screenProcessItemTypePowerOcrReadyMessageParams)
    {
    }

    public class ScreenProcessItemTypePowerOcrReadyMessageParams
    {
        public OcrResult OcrResultPower { get; set; } = new();
        public OcrResultItemType OcrResultItemType { get; set; } = new();
    }

    public class TooltipDataReadyMessage(TooltipDataReadyMessageParams tooltipDataReadyMessageParams) : ValueChangedMessage<TooltipDataReadyMessageParams>(tooltipDataReadyMessageParams)
    {
    }

    public class TooltipDataReadyMessageParams
    {
        public ItemTooltipDescriptor Tooltip { get; set; } = new ItemTooltipDescriptor();
    }
}
