namespace NPPE.Domain.Entities;

/// <summary>
/// Records the id of every Stripe webhook event we have processed, so a
/// redelivery of the same event is ignored (endpoint-wide idempotency).
/// </summary>
public class ProcessedStripeEvent : BaseEntity
{
    public string StripeEventId { get; set; } = string.Empty;
}
