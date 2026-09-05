using NPPE.Domain.Enums;

namespace NPPE.Application.DTOs.Finance;

public record FinancialSummaryDto
{
    public string PeriodLabel { get; init; } = string.Empty;

    // Headline figures for the selected period
    public decimal GrossRevenue { get; init; }
    public decimal StripeFees { get; init; }
    public decimal NetAfterFees { get; init; }
    public decimal InfraCosts { get; init; }   // manually recorded costs (excludes Stripe fees)
    public decimal NetProfit { get; init; }     // net after fees minus infra costs

    // Product split (period)
    public decimal ExamPackRevenue { get; init; }
    public int ExamPackCount { get; init; }     // number of pack purchases in the period
    public int ExamsSold { get; init; }         // total exams unlocked across those packs
    public decimal OneTimeRevenue { get; init; }   // legacy (retired lifetime product)
    public int OneTimeCount { get; init; }
    public decimal SubscriptionRevenue { get; init; }  // legacy (retired subscription)

    // Buyer counts (point-in-time, not period-bound)
    public int ExamPackBuyers { get; init; }
    public decimal Mrr { get; init; }              // legacy — 0 once subscriptions are gone
    public int ActiveSubscribers { get; init; }    // legacy
    public int OneTimeBuyers { get; init; }        // legacy

    public List<MonthlyRevenuePoint> OverTime { get; init; } = new();
    public List<CostLineDto> CostLedger { get; init; } = new();  // manual costs + a computed Stripe-fees line
    public List<RecentActivityDto> Recent { get; init; } = new();
}

public record MonthlyRevenuePoint(string Label, decimal ExamPack, decimal OneTime, decimal Subscription)
{
    public decimal Total => ExamPack + OneTime + Subscription;
}

public record CostLineDto(string Provider, string Category, decimal Amount, string Currency, bool IsComputed);

public record RecentActivityDto(DateTime Date, string Email, decimal Gross, decimal Fee, decimal Net, PaymentType Type);
