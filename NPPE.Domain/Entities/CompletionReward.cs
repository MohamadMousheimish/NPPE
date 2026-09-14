using NPPE.Domain.Constants;
using NPPE.Domain.Enums;

namespace NPPE.Domain.Entities;

/// <summary>
/// A one-time "thank-you" reward a student can claim after finishing ALL their exams
/// and leaving an on-platform review. Issued as a Stripe partial refund after admin
/// approval. One per student.
/// </summary>
public class CompletionReward : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = default!;

    /// <summary>The on-platform review this reward is anchored to.</summary>
    public Guid FeedbackId { get; set; }

    public RewardStatus Status { get; set; } = RewardStatus.Requested;

    /// <summary>Amount refunded (or to be refunded) in the account currency.</summary>
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = Currencies.Canadian;

    public string? StripeRefundId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByAdminId { get; set; }

    /// <summary>Admin note / rejection reason.</summary>
    public string? Note { get; set; }
}
