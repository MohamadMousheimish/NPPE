using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Commands.Feedback.SubmitFeedback;
using NPPE.Application.Queries.Feedback.GetMyFeedback;
using NPPE.Web.Resources;

namespace NPPE.Web.Pages.Student;

[Authorize(Policy = "StudentOnly")]
public class FeedbackModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public FeedbackModel(IMediator mediator, IStringLocalizer<SharedResource> localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public bool Eligible { get; private set; }
    public bool HasReview { get; private set; }
    public bool PendingApproval { get; private set; }

    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? throw new InvalidOperationException("User ID not found.");

    public async Task OnGetAsync()
    {
        var status = await _mediator.Send(new GetMyFeedbackQuery(UserId));
        Eligible = status.Eligible;
        if (status.Review is { } r)
        {
            HasReview = true;
            PendingApproval = !r.IsApproved;
            Input.Rating = r.Rating;
            Input.Comment = r.Comment;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var ok = await _mediator.Send(new SubmitFeedbackCommand(UserId, Input.Rating, Input.Comment));
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, _localizer["You can leave a review after completing at least one exam."]);
            await OnGetAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "Thanks! Your review was submitted and will appear once approved.";
        return RedirectToPage();
    }

    public class InputModel
    {
        [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5 stars.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please write a short review.")]
        [StringLength(1000, MinimumLength = 4, ErrorMessage = "Keep your review between 4 and 1000 characters.")]
        public string Comment { get; set; } = string.Empty;
    }
}
