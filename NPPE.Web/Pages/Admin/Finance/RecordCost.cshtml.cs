using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Finance.RecordCost;
using NPPE.Domain.Enums;

namespace NPPE.Web.Pages.Admin.Finance
{
    [Authorize(Roles = "Admin")]
    public class RecordCostModel : PageModel
    {
        private readonly IMediator _mediator;

        public RecordCostModel(IMediator mediator) => _mediator = mediator;

        [BindProperty] public InputModel Input { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public void OnGet() => Input.IncurredOn = DateTime.UtcNow.Date;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                await _mediator.Send(new RecordCostCommand(
                    Input.Provider, Input.Category, Input.Amount, Input.IncurredOn, Input.IsRecurring, Input.Note));
                TempData["SuccessMessage"] = "Cost recorded.";
                return RedirectToPage("Index");
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Provider is required.")]
            public string Provider { get; set; } = string.Empty;

            public CostCategory Category { get; set; } = CostCategory.Hosting;

            [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than zero.")]
            public decimal Amount { get; set; }

            [DataType(DataType.Date)]
            public DateTime IncurredOn { get; set; }

            public bool IsRecurring { get; set; }

            public string? Note { get; set; }
        }
    }
}
