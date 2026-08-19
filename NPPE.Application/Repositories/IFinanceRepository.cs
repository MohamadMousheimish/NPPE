using NPPE.Application.DTOs.Finance;
using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;

/// <summary>Read model for the admin finance dashboard — aggregates over payments and users.</summary>
public interface IFinanceRepository
{
    /// <summary>Succeeded payments whose PaidAt falls within the (optional) window. Null bounds are unbounded.</summary>
    Task<List<Payment>> GetSucceededPaymentsAsync(DateTime? fromInclusive, DateTime? toExclusive);

    /// <summary>Users with an active Stripe subscription (non-null id, not past its end date).</summary>
    Task<int> GetActiveSubscriberCountAsync();

    /// <summary>Distinct users with at least one succeeded one-time payment.</summary>
    Task<int> GetOneTimeBuyerCountAsync();

    /// <summary>Most recent succeeded payments joined to the customer email.</summary>
    Task<List<RecentActivityDto>> GetRecentActivityAsync(int take);
}
