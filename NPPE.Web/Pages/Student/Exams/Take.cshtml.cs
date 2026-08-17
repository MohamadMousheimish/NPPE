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

        // Maps each QuestionId to the selected AnswerOptionId.
        [BindProperty] public Dictionary<Guid, Guid> Answers { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (await RequirePremiumAsync(id) is { } redirect)
                return redirect;

            var exam = await _mediator.Send(new GetExamWithQuestionsQuery(id));
            if (exam == null)
                return NotFound();

            Exam = exam;
            ExamId = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? throw new InvalidOperationException("User ID not found.");

            // Premium can lapse between rendering the exam and submitting it, so
            // re-check before accepting the attempt.
            if (await RequirePremiumAsync(ExamId) is { } redirect)
                return redirect;

            // Re-load the exam so we can validate every question was answered
            // before attempting to submit (also repopulates the view on error).
            var exam = await _mediator.Send(new GetExamWithQuestionsQuery(ExamId));
            if (exam == null)
                return NotFound();

            Exam = exam;

            var unanswered = exam.Questions.Any(q => !Answers.ContainsKey(q.Id));
            if (unanswered)
            {
                ModelState.AddModelError(string.Empty, "Please answer all questions before submitting.");
                return Page();
            }

            var attemptId = await _mediator.Send(new SubmitExamAttemptCommand
            (
                studentId,
                ExamId,
                Answers
            ));

            return RedirectToPage("Results", new { id = attemptId });
        }

        // Returns a redirect to the pricing page when the current user is not a
        // premium member, or null when access is allowed.
        private async Task<IActionResult?> RequirePremiumAsync(Guid examId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? throw new InvalidOperationException("User ID not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsPremium)
            {
                return RedirectToPage("/Payments/Pricing", new { returnUrl = $"/Student/Exams/Take?id={examId}" });
            }

            return null;
        }
    }
}
