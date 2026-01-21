using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Exams.UpdateExam;
using NPPE.Application.Queries.Exams.GetAllExams;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Admin.Exams
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
            var exams = await _mediator.Send(new GetAllExamsQuery());
            var exam = exams.FirstOrDefault(e => e.Id == id);
            if (exam == null)
                return NotFound();

            Input = new InputModel
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                IsActive = exam.IsActive
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            await _mediator.Send(new UpdateExamCommand(Input.Id, Input.Title, Input.Description, Input.IsActive));

            TempData["SuccessMessage"] = "Exam updated successfully.";
            return RedirectToPage("Index");
        }

        public class InputModel
        {
            public Guid Id { get; set; }

            [Required(ErrorMessage = "Title is required.")]
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;

            [StringLength(2000)]
            public string Description { get; set; } = string.Empty;

            public bool IsActive { get; set; }
        }
    }
}
