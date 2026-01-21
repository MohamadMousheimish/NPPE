using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Payment?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
    }

    public async Task<List<Payment>> GetPaymentsByUserIdAsync(string userId)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
