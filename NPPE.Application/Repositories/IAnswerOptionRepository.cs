using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IAnswerOptionRepository : IGenericRepository<AnswerOption>
{
    Task<List<AnswerOption>> GetOptionsByQuestionIdAsync(Guid questionId);
}
