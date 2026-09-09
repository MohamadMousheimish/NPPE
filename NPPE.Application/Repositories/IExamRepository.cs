using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IExamRepository : IGenericRepository<Exam>
{
    Task<List<Exam>> GetActiveExamsAsync();
    Task<Exam?> GetExamWithQuestionsAsync(Guid id);

    /// <summary>The active exam with the given title, including its active questions and options.</summary>
    Task<Exam?> GetExamByTitleWithQuestionsAsync(string title);
}
