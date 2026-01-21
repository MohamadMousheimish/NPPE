using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Questions.GetQuestionsByExam;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Queries.Exams.GetExamById;

namespace NPPE.Web.Pages.Admin.Exams
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly IMediator _mediator;

        public DetailsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public ExamDto? Exam { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var exam = await _mediator.Send(new GetExamByIdQuery(id));
            if (exam == null)
                return NotFound();

            Exam = exam;
            Questions = await _mediator.Send(new GetQuestionsByExamQuery(id));
            return Page();
        }
    }
}
