using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;

public class CostRepository : GenericRepository<Cost>, ICostRepository
{
    public CostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Cost>> GetBetweenAsync(DateTime? fromInclusive, DateTime? toExclusive)
    {
        var q = _context.Set<Cost>().AsQueryable();
        if (fromInclusive.HasValue) q = q.Where(c => c.IncurredOn >= fromInclusive.Value);
        if (toExclusive.HasValue) q = q.Where(c => c.IncurredOn < toExclusive.Value);
        return await q.OrderByDescending(c => c.IncurredOn).ToListAsync();
    }
}
