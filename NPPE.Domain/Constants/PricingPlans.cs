namespace NPPE.Domain.Constants;
public static class PricingPlans
{
    public const decimal OneTimePrice = 29.00m;
    public const long OneTimePriceCents = 2900;

    public const decimal MonthlyPrice = 9.99m;
    public const long MonthlyPriceCents = 999;

    public const string OneTimeProductName = "NPPE Exam Prep Full Access";
    public const string OneTimeProductDescription = "One-time payment for unlimited exam attempts";

    public const string MonthlyProductName = "NPPE Exam Prep Monthly";
    public const string MonthlyProductDescription = "Monthly subscription for unlimited exam attempts";
}
