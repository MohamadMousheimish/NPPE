using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Questions.UpdateQuestion;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Queries.Questions.GetQuestionById;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Admin.Exams.Questions
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IMediator _mediator;

        public EditModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var question = await _mediator.Send(new GetQuestionByIdQuery(id));
            if (question == null)
                return NotFound();

            Input = new InputModel
            {
                Id = question.Id,
                ExamId = question.ExamId,
                Text = question.Text,
                ExplanationForCorrect = question.ExplanationForCorrect,
                ExplanationForIncorrect = question.ExplanationForIncorrect,
                Options = question.Options.Select(o => new OptionInput
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).OrderBy(o => o.Id).ToList() // Keep order consistent
            };

            // Ensure exactly 4 options (pad with empty if needed)
            var loadedOptions = question.Options
                .Select(o => new OptionInput
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                })
                .OrderBy(o => o.Id) // or by creation order if you store it
                .ToList();

            // Pad to 4 if missing (shouldn't happen, but safe)
            while (loadedOptions.Count < 4)
            {
                loadedOptions.Add(new OptionInput());
            }

            // Truncate to 4 if somehow more (shouldn't happen)
            Input.Options = loadedOptions.Take(4).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return await OnGetAsync(Input.Id);

            if (Input.Options.Count(o => o.IsCorrect) != 1)
            {
                ModelState.AddModelError(string.Empty, "Please select exactly one correct answer.");
                return Page();
            }

            await _mediator.Send(new UpdateQuestionCommand
            (
                Input.Id,
                Input.Text,
                Input.ExplanationForCorrect,
                Input.ExplanationForIncorrect,
                Input.Options.Select(o => new AnswerOptionDto
                (
                    o.Id,
                    o.Text, ' ',
                    o.IsCorrect
                )).ToList()
            ));

            TempData["SuccessMessage"] = "Question updated successfully.";
            return RedirectToPage("/Admin/Exams/Details", new { id = Input.ExamId });
        }

        public class InputModel
        {
            public Guid Id { get; set; }
            public Guid ExamId { get; set; }

            [Required] public string Text { get; set; } = string.Empty;
            [Required] public string ExplanationForCorrect { get; set; } = string.Empty;
            [Required] public string ExplanationForIncorrect { get; set; } = string.Empty;
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
