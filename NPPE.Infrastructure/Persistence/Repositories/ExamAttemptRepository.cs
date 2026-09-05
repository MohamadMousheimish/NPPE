using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class ExamAttemptRepository : GenericRepository<ExamAttempt>, IExamAttemptRepository
{

    public ExamAttemptRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ExamAttempt?> GetAttemptWithDetailsAsync(Guid attemptId)
    {
        return await _context.ExamAttempts
            .Include(a => a.Answers)
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
    }

    public async Task<List<ExamAttempt>> GetAttemptsByUserIdAsync(string userId)
    {
        return await _context.ExamAttempts
            .Include(a => a.Exam)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.TakenAt)
            .ToListAsync();
    }

    public async Task<int> CountAttemptsAsync(string userId, Guid examId)
    {
        return await _context.ExamAttempts
            .CountAsync(a => a.UserId == userId && a.ExamId == examId);
    }
}
