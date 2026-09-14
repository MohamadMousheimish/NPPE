using MediatR;
using NPPE.Application.Common;
using NPPE.Application.DTOs.Feedback;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Feedback.GetMyFeedback;

public record GetMyFeedbackQuery(string UserId) : IRequest<MyFeedbackStatusDto>;

public class GetMyFeedbackQueryHandler : IRequestHandler<GetMyFeedbackQuery, MyFeedbackStatusDto>
{
    private readonly IFeedbackRepository _feedback;
    private readonly IExamAttemptRepository _attempts;
    private readonly IExamUnlockRepository _unlocks;

    public GetMyFeedbackQueryHandler(
        IFeedbackRepository feedback, IExamAttemptRepository attempts, IExamUnlockRepository unlocks)
    {
        _feedback = feedback;
        _attempts = attempts;
        _unlocks = unlocks;
    }

    public async Task<MyFeedbackStatusDto> Handle(GetMyFeedbackQuery request, CancellationToken ct)
    {
        var eligible = await ReviewEligibility.HasFinishedAllExamsAsync(request.UserId, _unlocks, _attempts);
        var mine = await _feedback.GetByUserAsync(request.UserId);
        var review = mine == null ? null : new MyFeedbackDto(mine.Rating, mine.Comment, mine.IsApproved);
        return new MyFeedbackStatusDto(eligible, review);
    }
}
