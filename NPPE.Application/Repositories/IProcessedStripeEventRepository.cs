using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;

public interface IProcessedStripeEventRepository : IGenericRepository<ProcessedStripeEvent>
{
    Task<bool> ExistsAsync(string stripeEventId);
}
