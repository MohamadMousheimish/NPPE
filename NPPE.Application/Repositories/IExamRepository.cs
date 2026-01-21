using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IExamRepository : IGenericRepository<Exam>
{
    Task<List<Exam>> GetActiveExamsAsync();
    Task<Exam?> GetExamWithQuestionsAsync(Guid id);
}
