using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class ExamUnlockRepository : GenericRepository<ExamUnlock>, IExamUnlockRepository
{
    public ExamUnlockRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ExamUnlock>> GetForUserAsync(string userId) =>
        await _context.ExamUnlocks
            .Include(u => u.Exam)
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.UnlockedAt)
            .ToListAsync();

    public async Task<List<Guid>> GetUnlockedExamIdsAsync(string userId) =>
        await _context.ExamUnlocks.Where(u => u.UserId == userId).Select(u => u.ExamId).ToListAsync();

    public async Task<ExamUnlock?> GetAsync(string userId, Guid examId) =>
        await _context.ExamUnlocks.FirstOrDefaultAsync(u => u.UserId == userId && u.ExamId == examId);

    public async Task AddRangeAsync(IEnumerable<ExamUnlock> unlocks)
    {
        await _context.ExamUnlocks.AddRangeAsync(unlocks);
        await _context.SaveChangesAsync();
    }
}
