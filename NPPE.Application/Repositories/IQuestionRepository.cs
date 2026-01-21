using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IQuestionRepository : IGenericRepository<Question>
{
    Task<List<Question>> GetQuestionsByExamIdAsync(Guid examId);
    Task<Question?> GetQuestionWithOptionsAsync(Guid id);
}
