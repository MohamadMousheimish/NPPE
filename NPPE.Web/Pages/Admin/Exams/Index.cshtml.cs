using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetAllExams;

namespace NPPE.Web.Pages.Admin.Exams
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        public IndexModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public List<ExamDto> Exams { get; set; } = new();

        public async Task OnGetAsync()
        {
            Exams = await _mediator.Send(new GetAllExamsQuery());
        }
    }
}
