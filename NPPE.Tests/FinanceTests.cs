using Moq;
using NPPE.Application.Commands.Finance.RecordCost;
using NPPE.Application.DTOs.Finance;
using NPPE.Application.Queries.Finance.GetFinancialSummary;
using NPPE.Application.Queries.Finance.GetThresholdStatus;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Xunit;

namespace NPPE.Tests;

public class GstThresholdTests
{
    [Theory]
    [InlineData(0, ThresholdZone.Safe)]
    [InlineData(19999.99, ThresholdZone.Safe)]
    [InlineData(20000, ThresholdZone.Approaching)]
    [InlineData(24999.99, ThresholdZone.Approaching)]
    [InlineData(25000, ThresholdZone.RegisterNow)]
    [InlineData(31000, ThresholdZone.RegisterNow)]
    public void Classify_maps_amount_to_zone(decimal amount, ThresholdZone expected)
    {
        Assert.Equal(expected, GstThreshold.Classify(amount));
    }

    [Theory]
    [InlineData(29, 1.14)]      // 29*0.029 + 0.30
    [InlineData(9.99, 0.59)]    // 9.99*0.029 + 0.30
    public void Estimate_computes_stripe_fee(decimal amount, decimal expected)
    {
        Assert.Equal(expected, StripeFees.Estimate(amount));
    }
}

public class GetThresholdStatusQueryHandlerTests
{
    private static Payment Pay(decimal amt, string? country, DateTime at) =>
        new() { Amount = amt, CustomerCountry = country, Status = PaymentStatus.Succeeded, PaidAt = at, PaymentType = PaymentType.OneTime };

    [Fact]
    public async Task Counts_canadian_and_unknown_only_and_zones_correctly()
    {
        var asOf = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc);
        var finance = new Mock<IFinanceRepository>();
        finance.Setup(f => f.GetSucceededPaymentsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<Payment>
            {
                Pay(12000, "CA", asOf),
                Pay(9000, "ca", asOf),   // case-insensitive
                Pay(5000, "US", asOf),   // excluded (foreign, zero-rated)
                Pay(1000, null, asOf)    // unknown counts (conservative)
            });

        var result = await new GetThresholdStatusQueryHandler(finance.Object)
            .Handle(new GetThresholdStatusQuery(asOf), default);

        Assert.Equal(22000m, result.TaxableRevenue);         // 12000 + 9000 + 1000, US excluded
        Assert.Equal(ThresholdZone.Approaching, result.Zone); // 22k is in 20–25k
        Assert.Equal(8000m, result.Remaining);
        Assert.Equal(4, result.Quarters.Count);
    }
}

public class GetFinancialSummaryQueryHandlerTests
{
    private static Payment Pay(decimal amt, DateTime at, PaymentType type) =>
        new() { Amount = amt, Status = PaymentStatus.Succeeded, PaidAt = at, PaymentType = type };

    [Fact]
    public async Task Aggregates_gross_fees_net_costs_and_product_split()
    {
        var d = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var finance = new Mock<IFinanceRepository>();
        var costs = new Mock<ICostRepository>();

        finance.Setup(f => f.GetSucceededPaymentsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<Payment>
            {
                Pay(29m, d, PaymentType.OneTime),
                Pay(29m, d, PaymentType.OneTime),
                Pay(9.99m, d, PaymentType.Subscription)
            });
        costs.Setup(c => c.GetBetweenAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<Cost> { new() { Provider = "Azure", Category = CostCategory.Hosting, Amount = 156m, IncurredOn = d } });
        finance.Setup(f => f.GetActiveSubscriberCountAsync()).ReturnsAsync(1);
        finance.Setup(f => f.GetOneTimeBuyerCountAsync()).ReturnsAsync(2);
        finance.Setup(f => f.GetRecentActivityAsync(It.IsAny<int>())).ReturnsAsync(new List<RecentActivityDto>());

        var r = await new GetFinancialSummaryQueryHandler(finance.Object, costs.Object)
            .Handle(new GetFinancialSummaryQuery(null, null, "All time"), default);

        Assert.Equal(67.99m, r.GrossRevenue);        // 29 + 29 + 9.99
        Assert.Equal(2.87m, r.StripeFees);           // 1.14 + 1.14 + 0.59
        Assert.Equal(65.12m, r.NetAfterFees);        // 67.99 - 2.87
        Assert.Equal(156m, r.InfraCosts);
        Assert.Equal(-90.88m, r.NetProfit);          // net after fees minus infra
        Assert.Equal(2, r.OneTimeCount);
        Assert.Equal(58m, r.OneTimeRevenue);
        Assert.Equal(9.99m, r.SubscriptionRevenue);
        Assert.Equal(1, r.ActiveSubscribers);
        Assert.Equal(9.99m, r.Mrr);
        Assert.Contains(r.CostLedger, l => l.IsComputed && l.Provider == "Stripe"); // computed fee line added
    }
}

public class RecordCostCommandHandlerTests
{
    [Fact]
    public async Task Records_a_valid_cost()
    {
        var costs = new Mock<ICostRepository>();
        Cost? added = null;
        costs.Setup(c => c.AddAsync(It.IsAny<Cost>())).Callback<Cost>(c => added = c).Returns(Task.CompletedTask);

        var id = await new RecordCostCommandHandler(costs.Object).Handle(
            new RecordCostCommand("Azure", CostCategory.Hosting, 156m, new DateTime(2026, 5, 1), true, "  B1 plan  "), default);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(added);
        Assert.Equal("Azure", added!.Provider);
        Assert.Equal("B1 plan", added.Note); // trimmed
        Assert.Equal(CostSource.Manual, added.Source);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Azure", 0)]
    [InlineData("Azure", -5)]
    public async Task Rejects_invalid_input(string provider, decimal amount)
    {
        var costs = new Mock<ICostRepository>();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new RecordCostCommandHandler(costs.Object).Handle(
                new RecordCostCommand(provider, CostCategory.Hosting, amount, new DateTime(2026, 5, 1), false, null), default));
    }
}
