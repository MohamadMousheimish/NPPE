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

    // Product split
    public decimal OneTimeRevenue { get; init; }
    public int OneTimeCount { get; init; }
    public decimal SubscriptionRevenue { get; init; }

    // Subscription health (point-in-time, not period-bound)
    public decimal Mrr { get; init; }
    public int ActiveSubscribers { get; init; }
    public int OneTimeBuyers { get; init; }

    public List<MonthlyRevenuePoint> OverTime { get; init; } = new();
    public List<CostLineDto> CostLedger { get; init; } = new();  // manual costs + a computed Stripe-fees line
    public List<RecentActivityDto> Recent { get; init; } = new();
}

public record MonthlyRevenuePoint(string Label, decimal OneTime, decimal Subscription)
{
    public decimal Total => OneTime + Subscription;
}

public record CostLineDto(string Provider, string Category, decimal Amount, string Currency, bool IsComputed);

public record RecentActivityDto(DateTime Date, string Email, string Product, decimal Gross, decimal Fee, decimal Net, bool IsSubscription);
