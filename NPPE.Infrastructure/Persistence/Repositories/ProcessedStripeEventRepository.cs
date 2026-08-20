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

    public async Task<bool> TryAddAsync(string stripeEventId)
    {
        var entity = new ProcessedStripeEvent { StripeEventId = stripeEventId, CreatedAt = DateTime.UtcNow };
        _context.Set<ProcessedStripeEvent>().Add(entity);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique-index violation — another (concurrent or retried) delivery
            // already recorded this event. Detach so the context stays usable.
            _context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task RemoveAsync(string stripeEventId)
    {
        var entity = await _context.Set<ProcessedStripeEvent>()
            .FirstOrDefaultAsync(e => e.StripeEventId == stripeEventId);
        if (entity != null)
        {
            _context.Set<ProcessedStripeEvent>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
