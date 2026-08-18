using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;

public class ProcessedStripeEventRepository
    : GenericRepository<ProcessedStripeEvent>, IProcessedStripeEventRepository
{
    public ProcessedStripeEventRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(string stripeEventId)
    {
        return await _context.Set<ProcessedStripeEvent>()
            .AnyAsync(e => e.StripeEventId == stripeEventId);
    }
}
