using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface ICompletionRewardRepository : IGenericRepository<CompletionReward>
{
    Task<CompletionReward?> GetByUserAsync(string userId);
    Task<List<CompletionReward>> GetAllOrderedAsync();
}
