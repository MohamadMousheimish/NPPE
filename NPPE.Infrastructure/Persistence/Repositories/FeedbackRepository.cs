using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Feedback?> GetByUserAsync(string userId) =>
        await _context.Feedbacks.FirstOrDefaultAsync(f => f.UserId == userId);

    public async Task<List<Feedback>> GetApprovedAsync(int take) =>
        await _context.Feedbacks
            .Where(f => f.IsApproved)
            .OrderByDescending(f => f.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<List<Feedback>> GetAllOrderedAsync() =>
        await _context.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
}
