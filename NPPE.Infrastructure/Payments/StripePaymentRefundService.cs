using Microsoft.Extensions.Configuration;
using NPPE.Application.Services;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Infrastructure.Payments;

/// <summary>Stripe implementation: resolves the PaymentIntent behind a Checkout Session
/// and issues a (partial) refund against it.</summary>
public class StripePaymentRefundService : IPaymentRefundService
{
    private readonly IConfiguration _config;
    public StripePaymentRefundService(IConfiguration config) => _config = config;

    public async Task<string> RefundBySessionAsync(string stripeSessionId, long amountCents, CancellationToken ct = default)
    {
        if (amountCents <= 0) throw new ArgumentOutOfRangeException(nameof(amountCents));
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var session = await new SessionService().GetAsync(
            stripeSessionId,
            new SessionGetOptions { Expand = new List<string> { "payment_intent" } },
            cancellationToken: ct);

        var paymentIntentId = session.PaymentIntentId;
        if (string.IsNullOrEmpty(paymentIntentId))
            throw new InvalidOperationException($"No payment intent found for Checkout session {stripeSessionId}.");

        var refund = await new RefundService().CreateAsync(
            new RefundCreateOptions { PaymentIntent = paymentIntentId, Amount = amountCents },
            cancellationToken: ct);

        return refund.Id;
    }
}
