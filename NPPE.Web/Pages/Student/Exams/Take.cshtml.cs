using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.ExamAttempts.SubmitExamAttempt;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetExamWithQuestions;
using NPPE.Domain.Entities;

namespace NPPE.Web.Pages.Student.Exams
{
    [Authorize(Policy = "StudentOnly")]
    public class TakeModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly UserManager<AppUser> _userManager;

        public TakeModel(IMediator mediator, UserManager<AppUser> userManager)
        {
            _mediator = mediator;
            _userManager = userManager;
        }

        public ExamWithQuestionsDto? Exam { get; set; }
        [BindProperty] public Guid ExamId { get; set; }

        [BindProperty] public List<Guid> SelectedOptionIds { get; set; } = [];
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? throw new InvalidOperationException("User ID not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsPremium)
            {
                return RedirectToPage("/Payments/Checkout", new { returnUrl = $"/Student/Exams/Take?id={id}" });
            }

            var exam = await _mediator.Send(new GetExamWithQuestionsQuery(id));
            if (exam == null)
                return NotFound();

            Exam = exam;
            ExamId = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedOptionIds == null || SelectedOptionIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please answer all questions.");
                return await OnGetAsync(ExamId);
            }

            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found.");
            var attemptId = await _mediator.Send(new SubmitExamAttemptCommand
            (
                studentId,
                ExamId,
                SelectedOptionIds
            ));

            return RedirectToPage("Results", new { id = attemptId });
        }
    }
}
