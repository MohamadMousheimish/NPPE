using NPPE.Domain.Constants;
using NPPE.Domain.Enums;

namespace NPPE.Domain.Entities;
public class Payment : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = default!;
    public string StripeSessionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Currencies.Canadian;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaidAt { get; set; }

    public PaymentType PaymentType { get; set; } = PaymentType.OneTime;
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }

    /// <summary>Stripe invoice id for subscription renewals — used to dedupe redelivered webhooks.</summary>
    public string? StripeInvoiceId { get; set; }

    /// <summary>ISO-2 country of the customer (from the Stripe Checkout session); drives the "Canadian sales" tax threshold.</summary>
    public string? CustomerCountry { get; set; }
}
