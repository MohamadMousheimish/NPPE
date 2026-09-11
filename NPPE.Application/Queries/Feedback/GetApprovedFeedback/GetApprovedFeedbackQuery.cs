using MediatR;
using NPPE.Application.DTOs.Feedback;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Feedback.GetApprovedFeedback;

/// <summary>Approved testimonials for the public landing carousel.</summary>
public record GetApprovedFeedbackQuery(int Take = 24) : IRequest<List<FeedbackDto>>;

public class GetApprovedFeedbackQueryHandler : IRequestHandler<GetApprovedFeedbackQuery, List<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedback;
    public GetApprovedFeedbackQueryHandler(IFeedbackRepository feedback) => _feedback = feedback;

    public async Task<List<FeedbackDto>> Handle(GetApprovedFeedbackQuery request, CancellationToken ct)
    {
        var items = await _feedback.GetApprovedAsync(request.Take);
        return items.Select(f => new FeedbackDto(f.AuthorName, f.AuthorTitle, f.Rating, f.Comment)).ToList();
    }
}
