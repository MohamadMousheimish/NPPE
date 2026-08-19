using NPPE.Domain.Constants;
using NPPE.Domain.Enums;

namespace NPPE.Application.DTOs.Finance;

public record ThresholdStatusDto
{
    public decimal TaxableRevenue { get; init; }         // Canadian gross over the rolling 4 quarters
    public decimal Threshold { get; init; } = GstThreshold.Threshold;
    public decimal CautionAt { get; init; } = GstThreshold.CautionAt;
    public decimal DangerAt { get; init; } = GstThreshold.DangerAt;
    public decimal Remaining { get; init; }
    public double Percent { get; init; }                 // 0..1 of the threshold
    public ThresholdZone Zone { get; init; }
    public List<QuarterPoint> Quarters { get; init; } = new();
}

public record QuarterPoint(string Label, decimal Amount);
