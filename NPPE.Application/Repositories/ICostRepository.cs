using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;

public interface ICostRepository : IGenericRepository<Cost>
{
    /// <summary>Costs incurred within the (optional) window, newest first. Null bounds are unbounded.</summary>
    Task<List<Cost>> GetBetweenAsync(DateTime? fromInclusive, DateTime? toExclusive);
}
