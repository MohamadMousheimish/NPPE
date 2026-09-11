using MediatR;
using NPPE.Application.DTOs.Feedback;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Feedback.GetMyFeedback;

public record GetMyFeedbackQuery(string UserId) : IRequest<MyFeedbackStatusDto>;

public class GetMyFeedbackQueryHandler : IRequestHandler<GetMyFeedbackQuery, MyFeedbackStatusDto>
{
    private readonly IFeedbackRepository _feedback;
    private readonly IExamAttemptRepository _attempts;

    public GetMyFeedbackQueryHandler(IFeedbackRepository feedback, IExamAttemptRepository attempts)
    {
        _feedback = feedback;
        _attempts = attempts;
    }

    public async Task<MyFeedbackStatusDto> Handle(GetMyFeedbackQuery request, CancellationToken ct)
    {
        var eligible = (await _attempts.GetAttemptsByUserIdAsync(request.UserId)).Count > 0;
        var mine = await _feedback.GetByUserAsync(request.UserId);
        var review = mine == null ? null : new MyFeedbackDto(mine.Rating, mine.Comment, mine.IsApproved);
        return new MyFeedbackStatusDto(eligible, review);
    }
}
