namespace NPPE.Application.DTOs.Payments;

public record SubscriptionStatusDto
{
    public bool IsPremium { get; init; }
    public bool HasSubscription { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public bool IsCancelPending { get; init; }
}
