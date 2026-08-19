using NPPE.Domain.Enums;

namespace NPPE.Domain.Constants;

/// <summary>
/// CRA GST/HST small-supplier threshold and the dashboard's warning marks. The
/// gauge measures taxable revenue (gross sales to Canadian customers) over the
/// rolling four calendar quarters against these.
/// </summary>
public static class GstThreshold
{
    public const decimal Threshold = 30_000m;
    public const decimal CautionAt = 20_000m; // gauge turns orange
    public const decimal DangerAt = 25_000m;  // gauge turns red

    public static ThresholdZone Classify(decimal taxableRevenue) =>
        taxableRevenue >= DangerAt ? ThresholdZone.RegisterNow :
        taxableRevenue >= CautionAt ? ThresholdZone.Approaching :
        ThresholdZone.Safe;
}
