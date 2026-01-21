using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IExamAttemptRepository : IGenericRepository<ExamAttempt>
{
    Task<ExamAttempt?> GetAttemptWithDetailsAsync(Guid attemptId);

    Task<List<ExamAttempt>> GetAttemptsByUserIdAsync(string userId);
}
