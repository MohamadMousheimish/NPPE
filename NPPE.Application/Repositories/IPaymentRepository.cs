using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetBySessionIdAsync(string sessionId);
    Task<List<Payment>> GetPaymentsByUserIdAsync(string userId);
    Task<Payment?> GetBySubscriptionIdAsync(string subscriptionId);
    Task<bool> HasSucceededOneTimePaymentAsync(string userId);
    Task<bool> HasPaymentForInvoiceAsync(string invoiceId);

    /// <summary>
    /// Atomically transitions a payment from Pending → Succeeded for the given checkout
    /// session, in a single UPDATE. Returns true only for the caller that won the race —
    /// so the exam-pack grant runs exactly once even if the success page and the webhook
    /// both fire for the same purchase.
    /// </summary>
    Task<bool> TryClaimForGrantAsync(string sessionId);
}
