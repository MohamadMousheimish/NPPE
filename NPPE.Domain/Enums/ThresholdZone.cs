namespace NPPE.Domain.Enums;

/// <summary>GST/HST small-supplier threshold zone, driving the dashboard gauge colour.</summary>
public enum ThresholdZone
{
    Safe,          // below the caution mark
    Approaching,   // caution → danger (orange)
    RegisterNow    // at/above the danger mark (red)
}
