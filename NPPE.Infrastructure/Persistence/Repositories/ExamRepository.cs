using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class ExamRepository : GenericRepository<Exam>, IExamRepository
{
    public ExamRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Exam>> GetActiveExamsAsync()
    {
        return await _context.Exams
            .Where(e => e.IsActive)
            .ToListAsync();
    }

    public async Task<Exam?> GetExamWithQuestionsAsync(Guid id)
    {
        return await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
