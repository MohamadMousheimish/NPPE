using MediatR;
using NPPE.Application.DTOs.Finance;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;

namespace NPPE.Application.Queries.Finance.GetThresholdStatus;

/// <summary>AsOfUtc is passed in (rather than read from the clock) so the rolling window is testable.</summary>
public record GetThresholdStatusQuery(DateTime AsOfUtc) : IRequest<ThresholdStatusDto>;

public class GetThresholdStatusQueryHandler : IRequestHandler<GetThresholdStatusQuery, ThresholdStatusDto>
{
    private readonly IFinanceRepository _finance;

    public GetThresholdStatusQueryHandler(IFinanceRepository finance) => _finance = finance;

    public async Task<ThresholdStatusDto> Handle(GetThresholdStatusQuery request, CancellationToken ct)
    {
        var currentQuarterStart = QuarterStart(request.AsOfUtc);
        var windowStart = currentQuarterStart.AddMonths(-9); // four quarters incl. the current one

        var payments = await _finance.GetSucceededPaymentsAsync(windowStart, request.AsOfUtc);

        // Taxable revenue = gross sales to Canadian customers; unknown country counts
        // (conservative — never under-reports toward the threshold).
        var taxable = payments.Where(p => IsCanadianOrUnknown(p.CustomerCountry)).ToList();

        var quarters = new List<QuarterPoint>();
        for (var i = 0; i < 4; i++)
        {
            var qStart = windowStart.AddMonths(i * 3);
            var qEnd = qStart.AddMonths(3);
            var amount = taxable.Where(p => p.PaidAt >= qStart && p.PaidAt < qEnd).Sum(p => p.Amount);
            quarters.Add(new QuarterPoint(QuarterLabel(qStart), amount));
        }

        var total = quarters.Sum(q => q.Amount);
        return new ThresholdStatusDto
        {
            TaxableRevenue = total,
            Remaining = Math.Max(0, GstThreshold.Threshold - total),
            Percent = (double)(total / GstThreshold.Threshold),
            Zone = GstThreshold.Classify(total),
            Quarters = quarters
        };
    }

    private static bool IsCanadianOrUnknown(string? country) =>
        string.IsNullOrEmpty(country) || country.Equals("CA", StringComparison.OrdinalIgnoreCase);

    private static DateTime QuarterStart(DateTime d) =>
        new(d.Year, ((d.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static string QuarterLabel(DateTime qStart) => $"Q{(qStart.Month - 1) / 3 + 1} '{qStart:yy}";
}
