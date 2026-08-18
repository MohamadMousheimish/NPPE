using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
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

    public async Task<Payment?> GetBySubscriptionIdAsync(string subscriptionId)
    {
        return await _context.Payments
            .Where(p => p.StripeSubscriptionId == subscriptionId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasSucceededOneTimePaymentAsync(string userId)
    {
        return await _context.Payments
            .AnyAsync(p => p.UserId == userId
                && p.PaymentType == Domain.Enums.PaymentType.OneTime
                && p.Status == Domain.Enums.PaymentStatus.Succeeded);
    }

    public async Task<bool> HasPaymentForInvoiceAsync(string invoiceId)
    {
        return await _context.Payments
            .AnyAsync(p => p.StripeInvoiceId == invoiceId);
    }
}
