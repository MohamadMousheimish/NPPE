using MediatR;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Feedback.ModerateFeedback;

public record SetFeedbackApprovalCommand(Guid Id, bool Approved) : IRequest;
public record DeleteFeedbackCommand(Guid Id) : IRequest;

/// <summary>Create (Id null) or edit an admin-authored testimonial.</summary>
public record UpsertAdminFeedbackCommand(
    Guid? Id, string AuthorName, string? AuthorTitle, int Rating, string Comment, bool Approved) : IRequest;

public class SetFeedbackApprovalCommandHandler : IRequestHandler<SetFeedbackApprovalCommand>
{
    private readonly IFeedbackRepository _feedback;
    public SetFeedbackApprovalCommandHandler(IFeedbackRepository feedback) => _feedback = feedback;

    public async Task Handle(SetFeedbackApprovalCommand request, CancellationToken ct)
    {
        var f = await _feedback.GetByIdAsync(request.Id);
        if (f == null) return;
        f.IsApproved = request.Approved;
        f.UpdatedAt = DateTime.UtcNow;
        await _feedback.UpdateAsync(f);
    }
}

public class DeleteFeedbackCommandHandler : IRequestHandler<DeleteFeedbackCommand>
{
    private readonly IFeedbackRepository _feedback;
    public DeleteFeedbackCommandHandler(IFeedbackRepository feedback) => _feedback = feedback;

    public async Task Handle(DeleteFeedbackCommand request, CancellationToken ct)
    {
        var f = await _feedback.GetByIdAsync(request.Id);
        if (f != null) await _feedback.DeleteAsync(f);
    }
}

public class UpsertAdminFeedbackCommandHandler : IRequestHandler<UpsertAdminFeedbackCommand>
{
    private readonly IFeedbackRepository _feedback;
    public UpsertAdminFeedbackCommandHandler(IFeedbackRepository feedback) => _feedback = feedback;

    public async Task Handle(UpsertAdminFeedbackCommand request, CancellationToken ct)
    {
        var rating = Math.Clamp(request.Rating, 1, 5);
        var name = request.AuthorName.Trim();
        var title = string.IsNullOrWhiteSpace(request.AuthorTitle) ? null : request.AuthorTitle.Trim();
        var comment = request.Comment.Trim();

        if (request.Id is { } id)
        {
            var f = await _feedback.GetByIdAsync(id);
            if (f == null) return;
            f.AuthorName = name;
            f.AuthorTitle = title;
            f.Rating = rating;
            f.Comment = comment;
            f.IsApproved = request.Approved;
            f.UpdatedAt = DateTime.UtcNow;
            await _feedback.UpdateAsync(f);
        }
        else
        {
            await _feedback.AddAsync(new NPPE.Domain.Entities.Feedback
            {
                UserId = null, // admin-seeded
                AuthorName = name,
                AuthorTitle = title,
                Rating = rating,
                Comment = comment,
                IsApproved = request.Approved
            });
        }
    }
}
