namespace NPPE.Domain.Constants;

/// <summary>
/// Estimated Stripe processing fee (Canada domestic card): 2.9% + CA$0.30 per
/// successful charge. Used to derive net revenue from our own payment records
/// without calling the Stripe Balance API. This is an estimate, not the exact fee.
/// </summary>
public static class StripeFees
{
    public const decimal Percent = 0.029m;
    public const decimal Fixed = 0.30m;

    public static decimal Estimate(decimal amount) => Math.Round(amount * Percent + Fixed, 2);
}
