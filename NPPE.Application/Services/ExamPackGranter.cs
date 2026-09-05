using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;

namespace NPPE.Application.Services;

public class ExamPackGranter : IExamPackGranter
{
    private readonly IPaymentRepository _payments;
    private readonly IExamUnlockRepository _unlocks;
    private readonly IExamRepository _exams;
    private readonly IUnitOfWork _uow;

    public ExamPackGranter(
        IPaymentRepository payments,
        IExamUnlockRepository unlocks,
        IExamRepository exams,
        IUnitOfWork uow)
    {
        _payments = payments;
        _unlocks = unlocks;
        _exams = exams;
        _uow = uow;
    }

    public async Task<bool> GrantAsync(string stripeSessionId, string? customerCountry = null)
    {
        if (string.IsNullOrEmpty(stripeSessionId)) return false;

        var granted = false;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            // Atomically claim the payment (Pending → Succeeded). Only the winner grants,
            // so the success page and the webhook can't both unlock the same purchase.
            if (!await _payments.TryClaimForGrantAsync(stripeSessionId))
                return;

            var payment = await _payments.GetBySessionIdAsync(stripeSessionId);
            if (payment == null || payment.ExamsPurchased <= 0)
                return;

            if (!string.IsNullOrEmpty(customerCountry))
            {
                payment.CustomerCountry = customerCountry;
                await _payments.UpdateAsync(payment);
            }

            // Unlock the latest active exams the buyer doesn't already have.
            var alreadyUnlocked = await _unlocks.GetUnlockedExamIdsAsync(payment.UserId);
            var toUnlock = (await _exams.GetActiveExamsAsync())
                .Where(e => !alreadyUnlocked.Contains(e.Id))
                .OrderByDescending(e => e.CreatedAt)
                .Take(payment.ExamsPurchased)
                .Select(e => new ExamUnlock
                {
                    UserId = payment.UserId,
                    ExamId = e.Id,
                    AttemptsAllowed = PricingPlans.AttemptsPerExam,
                    UnlockedAt = DateTime.UtcNow
                })
                .ToList();

            if (toUnlock.Count > 0)
                await _unlocks.AddRangeAsync(toUnlock);

            granted = true;
        });

        return granted;
    }
}
