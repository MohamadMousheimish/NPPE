using Microsoft.EntityFrameworkCore;
using NPPE.Application.DTOs.Finance;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;

namespace NPPE.Infrastructure.Persistence.Repositories;

public class FinanceRepository : IFinanceRepository
{
    private readonly ApplicationDbContext _context;

    public FinanceRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<Payment>> GetSucceededPaymentsAsync(DateTime? fromInclusive, DateTime? toExclusive)
    {
        var q = _context.Payments.Where(p => p.Status == PaymentStatus.Succeeded);
        if (fromInclusive.HasValue) q = q.Where(p => p.PaidAt >= fromInclusive.Value);
        if (toExclusive.HasValue) q = q.Where(p => p.PaidAt < toExclusive.Value);
        return await q.OrderBy(p => p.PaidAt).ToListAsync();
    }

    public async Task<int> GetActiveSubscriberCountAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Users.CountAsync(u => u.StripeSubscriptionId != null
            && (u.SubscriptionEndDate == null || u.SubscriptionEndDate > now));
    }

    public async Task<int> GetOneTimeBuyerCountAsync()
    {
        return await _context.Payments
            .Where(p => p.PaymentType == PaymentType.OneTime && p.Status == PaymentStatus.Succeeded)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync();
    }

    public async Task<List<RecentActivityDto>> GetRecentActivityAsync(int take)
    {
        var rows = await (
            from p in _context.Payments
            where p.Status == PaymentStatus.Succeeded
            join u in _context.Users on p.UserId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby p.PaidAt descending
            select new { p.PaidAt, p.Amount, p.PaymentType, Email = u != null ? u.Email : null })
            .Take(take)
            .ToListAsync();

        return rows.Select(r =>
        {
            var fee = StripeFees.Estimate(r.Amount);
            var isSub = r.PaymentType == PaymentType.Subscription;
            return new RecentActivityDto(r.PaidAt, r.Email ?? "—",
                isSub ? "Monthly" : "Full Access", r.Amount, fee, r.Amount - fee, isSub);
        }).ToList();
    }
}
