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
}
