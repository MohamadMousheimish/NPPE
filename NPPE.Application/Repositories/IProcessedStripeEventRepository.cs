using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;

public interface IProcessedStripeEventRepository : IGenericRepository<ProcessedStripeEvent>
{
    /// <summary>
    /// Atomically claims an event id via the unique index. Returns true if this
    /// call recorded it, false if it was already recorded (a duplicate or
    /// concurrent delivery lost the race) — the caller should then skip processing.
    /// </summary>
    Task<bool> TryAddAsync(string stripeEventId);

    /// <summary>Releases a claim so a failed event can be reprocessed on Stripe's retry.</summary>
    Task RemoveAsync(string stripeEventId);
}
