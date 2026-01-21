using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Questions.DeleteQuestion;
using NPPE.Application.Queries.Questions.GetQuestionById;

namespace NPPE.Web.Pages.Admin.Exams.Questions
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly IMediator _mediator;

        public DeleteModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ExamId { get; set; }

        public string QuestionText { get; set; } = "Unknown";

        public async Task<IActionResult> OnGetAsync()
        {
            var question = await _mediator.Send(new GetQuestionByIdQuery(Id));
            if (question == null)
                return NotFound();

            QuestionText = question.Text;
            ExamId = question.ExamId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _mediator.Send(new DeleteQuestionCommand(Id));
            TempData["SuccessMessage"] = "Question deleted successfully.";
            return RedirectToPage("/Admin/Exams/Details", new { id = ExamId });
        }
    }
}
