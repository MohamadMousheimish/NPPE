using MediatR;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Common;
using NPPE.Application.Repositories;
using NPPE.Application.Services;
using NPPE.Domain.Enums;

namespace NPPE.Application.Commands.Rewards.ModerateReward;

/// <summary>Admin approves a claim: issues a Stripe partial refund (RefundPercent% of each
/// succeeded exam-pack payment) and marks the reward Refunded. Returns false if the reward
/// isn't in Requested state or nothing could be refunded.</summary>
public record ApproveCompletionRewardCommand(Guid RewardId, string AdminId) : IRequest<bool>;

/// <summary>Admin declines a claim with an optional reason.</summary>
public record RejectCompletionRewardCommand(Guid RewardId, string AdminId, string? Reason) : IRequest<bool>;

public class ApproveCompletionRewardCommandHandler : IRequestHandler<ApproveCompletionRewardCommand, bool>
{
    private readonly ICompletionRewardRepository _rewards;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentRefundService _refunds;
    private readonly IConfiguration _config;

    public ApproveCompletionRewardCommandHandler(
        ICompletionRewardRepository rewards, IPaymentRepository payments,
        IPaymentRefundService refunds, IConfiguration config)
    {
        _rewards = rewards;
        _payments = payments;
        _refunds = refunds;
        _config = config;
    }

    public async Task<bool> Handle(ApproveCompletionRewardCommand request, CancellationToken ct)
    {
        var reward = await _rewards.GetByIdAsync(request.RewardId);
        if (reward == null || reward.Status != RewardStatus.Requested) return false;

        var packPayments = (await _payments.GetPaymentsByUserIdAsync(reward.UserId))
            .Where(p => p.PaymentType == PaymentType.ExamPack && p.Status == PaymentStatus.Succeeded)
            .ToList();

        var percent = RewardPolicy.RefundPercent(_config);
        decimal refundedTotal = 0m;
        string? lastRefundId = null;

        foreach (var p in packPayments)
        {
            var cents = (long)Math.Round(p.Amount * percent, MidpointRounding.AwayFromZero); // Amount(dollars) * percent = 10%-of-Amount in cents
            if (cents <= 0) continue;
            lastRefundId = await _refunds.RefundBySessionAsync(p.StripeSessionId, cents, ct);
            refundedTotal += cents / 100m;
        }

        if (refundedTotal <= 0m) return false;

        reward.Status = RewardStatus.Refunded;
        reward.RefundAmount = refundedTotal;
        reward.StripeRefundId = lastRefundId;
        reward.ProcessedAt = DateTime.UtcNow;
        reward.ProcessedByAdminId = request.AdminId;
        await _rewards.UpdateAsync(reward);
        return true;
    }
}

public class RejectCompletionRewardCommandHandler : IRequestHandler<RejectCompletionRewardCommand, bool>
{
    private readonly ICompletionRewardRepository _rewards;
    public RejectCompletionRewardCommandHandler(ICompletionRewardRepository rewards) => _rewards = rewards;

    public async Task<bool> Handle(RejectCompletionRewardCommand request, CancellationToken ct)
    {
        var reward = await _rewards.GetByIdAsync(request.RewardId);
        if (reward == null || reward.Status != RewardStatus.Requested) return false;

        reward.Status = RewardStatus.Rejected;
        reward.Note = request.Reason?.Trim();
        reward.ProcessedAt = DateTime.UtcNow;
        reward.ProcessedByAdminId = request.AdminId;
        await _rewards.UpdateAsync(reward);
        return true;
    }
}
