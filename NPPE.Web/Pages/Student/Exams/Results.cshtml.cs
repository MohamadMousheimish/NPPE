using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.ExamAttempts.GetExamAttemptWithDetails;
using NPPE.Application.DTOs.ExamAttempts;

namespace NPPE.Web.Pages.Student.Exams
{
    public class ResultsModel : PageModel
    {
        private readonly IMediator _mediator;

        public ResultsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public ExamAttemptDetailsDto? Attempt { get; set; }
        public Guid ExamId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var attempt = await _mediator.Send(new GetExamAttemptWithDetailsQuery(id));
            if (attempt == null) return NotFound();

            Attempt = attempt;
            ExamId = attempt.ExamId;
            return Page();
        }
    }
}
