using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetBySessionIdAsync(string sessionId);
    Task<List<Payment>> GetPaymentsByUserIdAsync(string userId);
}
