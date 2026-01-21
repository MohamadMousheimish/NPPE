using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.ExamAttempts.GetStudentExamHistory;
using NPPE.Application.DTOs.ExamAttempts;

namespace NPPE.Web.Pages.Student.History
{
    [Authorize(Policy = "StudentOnly")]
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        public IndexModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public List<ExamAttemptSummaryDto> Attempts { get; set; } = [];

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? throw new InvalidOperationException("User ID not found.");

            Attempts = await _mediator.Send(new GetStudentExamHistoryQuery(userId));
        }
    }
}
