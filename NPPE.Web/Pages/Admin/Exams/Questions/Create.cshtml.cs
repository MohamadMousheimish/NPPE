using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Questions.CreateQuestion;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Queries.Exams.GetExamById;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Admin.Exams.Questions
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IMediator _mediator;

        public CreateModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ExamTitle { get; set; } = "Unknown Exam";

        public async Task<IActionResult> OnGetAsync(Guid examId)
        {
            var exam = await _mediator.Send(new GetExamByIdQuery(examId));
            if (exam == null)
                return NotFound();

            ExamTitle = exam.Title;
            Input.ExamId = examId;
            Input.Options = Enumerable.Range(0, 4)
                .Select(_ => new OptionInput())
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return await OnGetAsync(Input.ExamId);

            var correctCount = Input.Options.Count(o => o.IsCorrect);
            if (correctCount != 1)
            {
                ModelState.AddModelError(string.Empty, "Please select exactly one correct answer.");
                return await OnGetAsync(Input.ExamId);
            }

            await _mediator.Send(new CreateQuestionCommand
            (
                Input.ExamId,
                Input.Text,
                Input.ExplanationForCorrect,
                Input.ExplanationForIncorrect,
                Input.Options.Select(o => new AnswerOptionDto
                (
                    o.Id,
                    o.Text,
                    ' ',
                    o.IsCorrect
                )).ToList()
            ));

            TempData["SuccessMessage"] = "Question added successfully.";
            return RedirectToPage("/Admin/Exams/Details", new { id = Input.ExamId });
        }

        public class InputModel
        {
            public Guid ExamId { get; set; }

            [Required(ErrorMessage = "Question text is required.")]
            [StringLength(2000)]
            public string Text { get; set; } = string.Empty;

            [Required(ErrorMessage = "Feedback for correct answer is required.")]
            public string ExplanationForCorrect { get; set; } = string.Empty;

            [Required(ErrorMessage = "Feedback for incorrect answer is required.")]
            public string ExplanationForIncorrect { get; set; } = string.Empty;

            public List<OptionInput> Options { get; set; } = new();
        }

        public class OptionInput
        {
            public Guid? Id { get; set; }
            public string Text { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }
    }
}
