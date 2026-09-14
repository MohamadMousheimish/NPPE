using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class CompletionRewardRepository : GenericRepository<CompletionReward>, ICompletionRewardRepository
{
    public CompletionRewardRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CompletionReward?> GetByUserAsync(string userId) =>
        await _context.CompletionRewards.FirstOrDefaultAsync(r => r.UserId == userId);

    public async Task<List<CompletionReward>> GetAllOrderedAsync() =>
        await _context.CompletionRewards
            .Include(r => r.User)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
}
