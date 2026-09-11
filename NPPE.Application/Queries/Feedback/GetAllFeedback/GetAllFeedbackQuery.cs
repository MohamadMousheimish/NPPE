using MediatR;
using NPPE.Application.DTOs.Feedback;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Feedback.GetAllFeedback;

/// <summary>All reviews (approved or not) for the admin moderation list.</summary>
public record GetAllFeedbackQuery() : IRequest<List<AdminFeedbackDto>>;

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, List<AdminFeedbackDto>>
{
    private readonly IFeedbackRepository _feedback;
    public GetAllFeedbackQueryHandler(IFeedbackRepository feedback) => _feedback = feedback;

    public async Task<List<AdminFeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken ct)
    {
        var items = await _feedback.GetAllOrderedAsync();
        return items.Select(f => new AdminFeedbackDto(
            f.Id, f.AuthorName, f.AuthorTitle, f.Rating, f.Comment,
            f.IsApproved, f.UserId != null, f.CreatedAt)).ToList();
    }
}
