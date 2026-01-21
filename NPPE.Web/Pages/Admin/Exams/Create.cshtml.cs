using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Exams.CreateExam;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Admin.Exams
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IMediator _mediator;

        public CreateModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var command = new CreateExamCommand(Input.Title, Input.Description, Input.IsActive);

            var examId = await _mediator.Send(command);
            SuccessMessage = $"Exam created successfully! ID: {examId}";
            return Page();
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Title is required.")]
            [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
            public string Title { get; set; } = string.Empty;

            [StringLength(2000, ErrorMessage = "Description is too long.")]
            public string Description { get; set; } = string.Empty;

            public bool IsActive { get; set; } = true;
        }
    }
}
