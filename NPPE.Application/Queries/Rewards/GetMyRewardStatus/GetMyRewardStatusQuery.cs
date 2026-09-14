using MediatR;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Common;
using NPPE.Application.DTOs.Rewards;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Rewards.GetMyRewardStatus;

public record GetMyRewardStatusQuery(string UserId) : IRequest<MyRewardStatusDto>;

public class GetMyRewardStatusQueryHandler : IRequestHandler<GetMyRewardStatusQuery, MyRewardStatusDto>
{
    private readonly ICompletionRewardRepository _rewards;
    private readonly IFeedbackRepository _feedback;
    private readonly IExamUnlockRepository _unlocks;
    private readonly IExamAttemptRepository _attempts;
    private readonly IPaymentRepository _payments;
    private readonly IConfiguration _config;

    public GetMyRewardStatusQueryHandler(
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

    public async Task<MyRewardStatusDto> Handle(GetMyRewardStatusQuery request, CancellationToken ct)
    {
        var enabled = RewardPolicy.IsEnabled(_config);
        var eligible = await ReviewEligibility.HasFinishedAllExamsAsync(request.UserId, _unlocks, _attempts);
        var hasReview = (await _feedback.GetByUserAsync(request.UserId)) != null;
        var amount = await RewardPolicy.ComputeRefundAsync(request.UserId, _payments, _config);
        var existing = await _rewards.GetByUserAsync(request.UserId);

        return new MyRewardStatusDto(
            Enabled: enabled,
            Eligible: eligible,
            HasReview: hasReview,
            Percent: RewardPolicy.RefundPercent(_config),
            RefundAmount: amount,
            Currency: "CAD",
            Status: existing?.Status.ToString());
    }
}
