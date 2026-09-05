using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Commands.ExamAttempts.SubmitExamAttempt;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetExamWithQuestions;
using NPPE.Application.Repositories;
using NPPE.Web.Resources;

namespace NPPE.Web.Pages.Student.Exams
{
    [Authorize(Policy = "StudentOnly")]
    public class TakeModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IExamUnlockRepository _examUnlocks;
        private readonly IExamAttemptRepository _examAttempts;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TakeModel(
            IMediator mediator,
            IExamUnlockRepository examUnlocks,
            IExamAttemptRepository examAttempts,
            IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _examUnlocks = examUnlocks;
            _examAttempts = examAttempts;
            _localizer = localizer;
        }

        public ExamWithQuestionsDto? Exam { get; set; }
        [BindProperty] public Guid ExamId { get; set; }

        // Maps each QuestionId to the selected AnswerOptionId.
        [BindProperty] public Dictionary<Guid, Guid> Answers { get; set; } = new();

        public int AttemptsRemaining { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (await RequireAccessAsync(id) is { } redirect)
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

            // Access (unlock + remaining attempts) can change between rendering and
            // submitting, so re-check before accepting the attempt.
            if (await RequireAccessAsync(ExamId) is { } redirect)
                return redirect;

            var exam = await _mediator.Send(new GetExamWithQuestionsQuery(ExamId));
            if (exam == null)
                return NotFound();

            Exam = exam;

            var unanswered = exam.Questions.Any(q => !Answers.ContainsKey(q.Id));
            if (unanswered)
            {
                ModelState.AddModelError(string.Empty, _localizer["Please answer all questions before submitting."]);
                return Page();
            }

            var attemptId = await _mediator.Send(new SubmitExamAttemptCommand(studentId, ExamId, Answers));
            return RedirectToPage("Results", new { id = attemptId });
        }

        // Returns a redirect when the student may not take this exam:
        //  - no unlock  -> pricing page
        //  - out of attempts -> exams list with a message
        // Otherwise returns null and sets AttemptsRemaining.
        private async Task<IActionResult?> RequireAccessAsync(Guid examId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? throw new InvalidOperationException("User ID not found.");

            var unlock = await _examUnlocks.GetAsync(userId, examId);
            if (unlock == null)
                return RedirectToPage("/Payments/Pricing", new { returnUrl = $"/Student/Exams/Take?id={examId}" });

            var used = await _examAttempts.CountAttemptsAsync(userId, examId);
            AttemptsRemaining = Math.Max(0, unlock.AttemptsAllowed - used);
            if (AttemptsRemaining <= 0)
            {
                TempData["ExamMessage"] = _localizer["You've used all your attempts for this exam."].Value;
                return RedirectToPage("/Student/Exams/Index");
            }

            return null;
        }
    }
}
