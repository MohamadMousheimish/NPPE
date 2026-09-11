using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IFeedbackRepository : IGenericRepository<Feedback>
{
    Task<Feedback?> GetByUserAsync(string userId);
    Task<List<Feedback>> GetApprovedAsync(int take);
    Task<List<Feedback>> GetAllOrderedAsync();
}
