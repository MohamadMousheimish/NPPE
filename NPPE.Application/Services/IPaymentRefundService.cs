namespace NPPE.Application.Services;

/// <summary>Issues refunds against a completed Stripe Checkout payment.
/// Abstracted so the reward flow is testable without calling Stripe.</summary>
public interface IPaymentRefundService
{
    /// <summary>Partially (or fully) refund the payment behind a Checkout Session.
    /// Returns the Stripe refund id.</summary>
    Task<string> RefundBySessionAsync(string stripeSessionId, long amountCents, CancellationToken ct = default);
}
