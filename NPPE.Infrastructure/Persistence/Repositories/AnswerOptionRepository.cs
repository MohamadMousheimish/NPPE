using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class AnswerOptionRepository : GenericRepository<AnswerOption>, IAnswerOptionRepository
{
    public AnswerOptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<AnswerOption>> GetOptionsByQuestionIdAsync(Guid questionId)
    {
        return await _context.AnswerOptions
            .Where(o => o.QuestionId == questionId)
            .ToListAsync();
    }
}