using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Feedback.ModerateFeedback;
using NPPE.Application.DTOs.Feedback;
using NPPE.Application.Queries.Feedback.GetAllFeedback;

namespace NPPE.Web.Pages.Admin.Feedback;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<AdminFeedbackDto> Items { get; private set; } = new();
    [BindProperty] public NewTestimonial New { get; set; } = new();

    public async Task OnGetAsync() => Items = await _mediator.Send(new GetAllFeedbackQuery());

    public async Task<IActionResult> OnPostApproveAsync(Guid id, bool approved)
    {
        await _mediator.Send(new SetFeedbackApprovalCommand(id, approved));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _mediator.Send(new DeleteFeedbackCommand(id));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }
        await _mediator.Send(new UpsertAdminFeedbackCommand(
            null, New.AuthorName, New.AuthorTitle, New.Rating, New.Comment, New.Approved));
        TempData["SuccessMessage"] = "Testimonial added.";
        return RedirectToPage();
    }

    public class NewTestimonial
    {
        [Required] [StringLength(120)] public string AuthorName { get; set; } = string.Empty;
        [StringLength(120)] public string? AuthorTitle { get; set; }
        [Range(1, 5)] public int Rating { get; set; } = 5;
        [Required] [StringLength(1000, MinimumLength = 4)] public string Comment { get; set; } = string.Empty;
        public bool Approved { get; set; } = true;
    }
}
