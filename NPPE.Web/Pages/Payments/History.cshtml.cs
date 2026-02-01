using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.DTOs.Payments;
using NPPE.Application.Queries.Payments.GetPaymentHistory;

namespace NPPE.Web.Pages.Payments;

[Authorize(Roles = "Student")]
public class HistoryModel : PageModel
{
    private readonly IMediator _mediator;

    public HistoryModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public List<PaymentHistoryDto> Payments { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? throw new InvalidOperationException("User ID not found.");
        Payments = await _mediator.Send(new GetPaymentHistoryQuery(userId));
    }
}
