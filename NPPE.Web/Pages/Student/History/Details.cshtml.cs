using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.ExamAttempts.GetExamAttemptWithDetails;
using NPPE.Application.DTOs.ExamAttempts;

namespace NPPE.Web.Pages.Student.History
{
    [Authorize(Policy = "StudentOnly")]
    public class DetailsModel : PageModel
    {
        private readonly IMediator _mediator;

        public DetailsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public ExamAttemptDetailsDto? Attempt { get; set; }
        public Guid ExamId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var attempt = await _mediator.Send(new GetExamAttemptWithDetailsQuery(id));
            if (attempt == null) return NotFound();
            if (attempt.StudentId != userId) return Unauthorized();

            Attempt = attempt;
            ExamId = attempt.ExamId;
            return Page();
        }
    }
}
