using MediatR;
using NPPE.Application.Common;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.Feedback.SubmitFeedback;

/// <summary>Upserts the signed-in student's own review. Returns false if the student
/// isn't eligible yet (must have finished ALL of their unlocked exams). One review per
/// student (upsert). Reviews go live immediately; an admin can hide or delete them later.</summary>
public record SubmitFeedbackCommand(string UserId, int Rating, string Comment) : IRequest<bool>;

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, bool>
{
    private readonly IFeedbackRepository _feedback;
    private readonly IExamAttemptRepository _attempts;
    private readonly IExamUnlockRepository _unlocks;
    private readonly IUserRepository _users;

    public SubmitFeedbackCommandHandler(
        IFeedbackRepository feedback, IExamAttemptRepository attempts,
        IExamUnlockRepository unlocks, IUserRepository users)
    {
        _feedback = feedback;
        _attempts = attempts;
        _unlocks = unlocks;
        _users = users;
    }

    public async Task<bool> Handle(SubmitFeedbackCommand request, CancellationToken ct)
    {
        if (!await ReviewEligibility.HasFinishedAllExamsAsync(request.UserId, _unlocks, _attempts))
            return false; // not eligible until all unlocked exams are finished

        var rating = Math.Clamp(request.Rating, 1, 5);
        var comment = (request.Comment ?? string.Empty).Trim();

        var existing = await _feedback.GetByUserAsync(request.UserId);
        if (existing != null)
        {
            existing.Rating = rating;
            existing.Comment = comment;
            existing.IsApproved = true; // reviews are visible immediately; admin can hide later
            existing.UpdatedAt = DateTime.UtcNow;
            await _feedback.UpdateAsync(existing);
        }
        else
        {
            var user = await _users.GetUserByIdAsync(request.UserId);
            await _feedback.AddAsync(new NPPE.Domain.Entities.Feedback
            {
                UserId = request.UserId,
                AuthorName = DisplayName(user),
                Rating = rating,
                Comment = comment,
                IsApproved = true
            });
        }
        return true;
    }

    /// <summary>First name + last initial, e.g. "Ada L." — privacy-friendly public attribution.</summary>
    private static string DisplayName(AppUser? u)
    {
        var first = (u?.FirstName ?? string.Empty).Trim();
        var last = (u?.LastName ?? string.Empty).Trim();
        if (first.Length == 0 && last.Length == 0) return "Student";
        return last.Length == 0 ? first : $"{first} {char.ToUpperInvariant(last[0])}.";
    }
}
