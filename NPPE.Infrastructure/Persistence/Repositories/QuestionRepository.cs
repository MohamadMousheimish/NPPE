using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
{
    public QuestionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Question>> GetQuestionsByExamIdAsync(Guid examId)
    {
        return await _context.Questions
            .Where(q => q.ExamId == examId)
            .ToListAsync();
    }

    public async Task<Question?> GetQuestionWithOptionsAsync(Guid id)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
    }
}
