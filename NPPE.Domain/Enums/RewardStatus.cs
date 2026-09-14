namespace NPPE.Domain.Enums;

/// <summary>Lifecycle of a student's 10% completion reward.</summary>
public enum RewardStatus
{
    /// <summary>Student claimed it (left a review); awaiting admin approval.</summary>
    Requested,
    /// <summary>Admin approved; a Stripe partial refund was issued.</summary>
    Refunded,
    /// <summary>Admin declined the claim.</summary>
    Rejected
}
