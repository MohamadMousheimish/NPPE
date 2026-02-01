namespace NPPE.Application.DTOs.Payments;

public record PaymentHistoryDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? SubscriptionStatus { get; init; }
}
