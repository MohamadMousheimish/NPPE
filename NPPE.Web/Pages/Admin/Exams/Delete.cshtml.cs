using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Exams.DeactivateExam;
using NPPE.Application.Queries.Exams.GetAllExams;

namespace NPPE.Web.Pages.Admin.Exams
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

        public string ExamTitle { get; set; } = "Unknown";

        public async Task<IActionResult> OnGetAsync()
        {
            var exams = await _mediator.Send(new GetAllExamsQuery());
            var exam = exams.FirstOrDefault(e => e.Id == Id);
            if (exam == null)
                return NotFound();

            ExamTitle = exam.Title;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _mediator.Send(new DeactivateExamCommand(Id));
            TempData["SuccessMessage"] = "Exam deleted successfully.";
            return RedirectToPage("Index");
        }
    }
}