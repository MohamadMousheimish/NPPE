using MediatR;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Common;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;

namespace NPPE.Application.Commands.Rewards.RequestCompletionReward;

/// <summary>Student claims their one-time completion reward. Requires: rewards enabled,
/// all unlocked exams finished, an on-platform review left, and a paid exam pack.
/// Idempotent — re-claiming when already pending is a no-op that still returns true.</summary>
public record RequestCompletionRewardCommand(string UserId) : IRequest<bool>;

public class RequestCompletionRewardCommandHandler : IRequestHandler<RequestCompletionRewardCommand, bool>
{
    private readonly ICompletionRewardRepository _rewards;
    private readonly IFeedbackRepository _feedback;
    private readonly IExamUnlockRepository _unlocks;
    private readonly IExamAttemptRepository _attempts;
    private readonly IPaymentRepository _payments;
    private readonly IConfiguration _config;

    public RequestCompletionRewardCommandHandler(
        ICompletionRewardRepository rewards, IFeedbackRepository feedback,
        IExamUnlockRepository unlocks, IExamAttemptRepository attempts,
        IPaymentRepository payments, IConfiguration config)
    {
        _rewards = rewards;
        _feedback = feedback;
        _unlocks = unlocks;
        _attempts = attempts;
        _payments = payments;
        _config = config;
    }

    public async Task<bool> Handle(RequestCompletionRewardCommand request, CancellationToken ct)
    {
        if (!RewardPolicy.IsEnabled(_config)) return false;
        if (!await ReviewEligibility.HasFinishedAllExamsAsync(request.UserId, _unlocks, _attempts))
            return false;

        var review = await _feedback.GetByUserAsync(request.UserId);
        if (review == null) return false; // must have left a review first

        var amount = await RewardPolicy.ComputeRefundAsync(request.UserId, _payments, _config);
        if (amount <= 0m) return false; // nothing paid to refund

        var existing = await _rewards.GetByUserAsync(request.UserId);
        if (existing != null) return true; // already claimed

        await _rewards.AddAsync(new CompletionReward
        {
            UserId = request.UserId,
            FeedbackId = review.Id,
            RefundAmount = amount,
            Status = RewardStatus.Requested,
            RequestedAt = DateTime.UtcNow
        });
        return true;
    }
}
