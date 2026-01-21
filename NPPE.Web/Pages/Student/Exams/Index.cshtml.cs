using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetAllExams;

namespace NPPE.Web.Pages.Student.Exams
{
    [Authorize(Policy = "StudentOnly")]
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
            // Only active exams
            var allExams = await _mediator.Send(new GetAllExamsQuery());
            Exams = allExams.Where(e => e.IsActive).ToList();
        }
    }
}
