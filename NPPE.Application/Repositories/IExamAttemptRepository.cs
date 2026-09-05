using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IExamAttemptRepository : IGenericRepository<ExamAttempt>
{
    Task<ExamAttempt?> GetAttemptWithDetailsAsync(Guid attemptId);

    Task<List<ExamAttempt>> GetAttemptsByUserIdAsync(string userId);

    /// <summary>Number of attempts a student has taken for a specific exam (for the per-exam attempt cap).</summary>
    Task<int> CountAttemptsAsync(string userId, Guid examId);
}
