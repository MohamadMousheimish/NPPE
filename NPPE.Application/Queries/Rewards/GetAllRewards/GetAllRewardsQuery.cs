using MediatR;
using NPPE.Application.DTOs.Rewards;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Rewards.GetAllRewards;

public record GetAllRewardsQuery() : IRequest<List<AdminRewardDto>>;

public class GetAllRewardsQueryHandler : IRequestHandler<GetAllRewardsQuery, List<AdminRewardDto>>
{
    private readonly ICompletionRewardRepository _rewards;
    private readonly IFeedbackRepository _feedback;

    public GetAllRewardsQueryHandler(ICompletionRewardRepository rewards, IFeedbackRepository feedback)
    {
        _rewards = rewards;
        _feedback = feedback;
    }

    public async Task<List<AdminRewardDto>> Handle(GetAllRewardsQuery request, CancellationToken ct)
    {
        var rewards = await _rewards.GetAllOrderedAsync();
        var list = new List<AdminRewardDto>(rewards.Count);
        foreach (var r in rewards)
        {
            var review = await _feedback.GetByUserAsync(r.UserId); // the anchoring on-platform review
            var name = ($"{r.User?.FirstName} {r.User?.LastName}").Trim();
            list.Add(new AdminRewardDto(
                Id: r.Id,
                UserName: string.IsNullOrWhiteSpace(name) ? "Student" : name,
                Email: r.User?.Email ?? string.Empty,
                Rating: review?.Rating,
                Comment: review?.Comment,
                RefundAmount: r.RefundAmount,
                Currency: r.Currency,
                Status: r.Status.ToString(),
                RequestedAt: r.RequestedAt,
                ProcessedAt: r.ProcessedAt));
        }
        return list;
    }
}
