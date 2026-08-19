using MediatR;
using NPPE.Application.DTOs.Finance;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Enums;

namespace NPPE.Application.Queries.Finance.GetFinancialSummary;

public record GetFinancialSummaryQuery(DateTime? From, DateTime? To, string PeriodLabel)
    : IRequest<FinancialSummaryDto>;

public class GetFinancialSummaryQueryHandler
    : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IFinanceRepository _finance;
    private readonly ICostRepository _costs;

    public GetFinancialSummaryQueryHandler(IFinanceRepository finance, ICostRepository costs)
    {
        _finance = finance;
        _costs = costs;
    }

    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken ct)
    {
        var payments = await _finance.GetSucceededPaymentsAsync(request.From, request.To);
        var costs = await _costs.GetBetweenAsync(request.From, request.To);

        var gross = payments.Sum(p => p.Amount);
        var fees = payments.Sum(p => StripeFees.Estimate(p.Amount));
        var infra = costs.Sum(c => c.Amount);
        var netAfterFees = gross - fees;

        var oneTime = payments.Where(p => p.PaymentType == PaymentType.OneTime).ToList();
        var subs = payments.Where(p => p.PaymentType == PaymentType.Subscription).ToList();

        // Revenue over time — by calendar month within the period.
        var overTime = payments
            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenuePoint(
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                g.Where(p => p.PaymentType == PaymentType.OneTime).Sum(p => p.Amount),
                g.Where(p => p.PaymentType == PaymentType.Subscription).Sum(p => p.Amount)))
            .ToList();

        // Cost ledger = manual costs (grouped by provider) + a computed Stripe-fees line.
        var ledger = costs
            .GroupBy(c => new { c.Provider, c.Category })
            .Select(g => new CostLineDto(g.Key.Provider, g.Key.Category.ToString(),
                g.Sum(x => x.Amount), Currencies.Canadian, false))
            .OrderByDescending(l => l.Amount)
            .ToList();
        if (fees > 0)
            ledger.Add(new CostLineDto("Stripe", "Payments", Math.Round(fees, 2), Currencies.Canadian, true));

        var activeSubs = await _finance.GetActiveSubscriberCountAsync();
        var oneTimeBuyers = await _finance.GetOneTimeBuyerCountAsync();
        var recent = await _finance.GetRecentActivityAsync(8);

        return new FinancialSummaryDto
        {
            PeriodLabel = request.PeriodLabel,
            GrossRevenue = gross,
            StripeFees = fees,
            NetAfterFees = netAfterFees,
            InfraCosts = infra,
            NetProfit = netAfterFees - infra,
            OneTimeRevenue = oneTime.Sum(p => p.Amount),
            OneTimeCount = oneTime.Count,
            SubscriptionRevenue = subs.Sum(p => p.Amount),
            Mrr = activeSubs * PricingPlans.MonthlyPrice,
            ActiveSubscribers = activeSubs,
            OneTimeBuyers = oneTimeBuyers,
            OverTime = overTime,
            CostLedger = ledger,
            Recent = recent
        };
    }
}
