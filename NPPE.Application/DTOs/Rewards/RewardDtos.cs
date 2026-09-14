namespace NPPE.Application.DTOs.Rewards;

/// <summary>Student-facing state for the reward panel.</summary>
public record MyRewardStatusDto(
    bool Enabled,
    bool Eligible,        // finished all unlocked exams
    bool HasReview,       // left an on-platform review
    int Percent,          // refund percentage (e.g. 5)
    decimal RefundAmount, // percent of what they paid
    string Currency,
    string? Status);      // null = not claimed yet; else Requested/Refunded/Rejected

/// <summary>Admin row for the rewards moderation list.</summary>
public record AdminRewardDto(
    Guid Id,
    string UserName,
    string Email,
    int? Rating,
    string? Comment,
    decimal RefundAmount,
    string Currency,
    string Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt);
