using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IExamUnlockRepository : IGenericRepository<ExamUnlock>
{
    Task<List<ExamUnlock>> GetForUserAsync(string userId);
    Task<List<Guid>> GetUnlockedExamIdsAsync(string userId);
    Task<ExamUnlock?> GetAsync(string userId, Guid examId);
    Task AddRangeAsync(IEnumerable<ExamUnlock> unlocks);
}
