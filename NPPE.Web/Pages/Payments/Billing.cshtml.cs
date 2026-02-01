using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.CancelSubscription;
using NPPE.Application.DTOs.Payments;
using NPPE.Application.Queries.Payments.GetSubscriptionStatus;

namespace NPPE.Web.Pages.Payments;

[Authorize(Roles = "Student")]
public class BillingModel : PageModel
{
    private readonly IMediator _mediator;

    public BillingModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public SubscriptionStatusDto? SubscriptionStatus { get; set; }
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        SubscriptionStatus = await _mediator.Send(new GetSubscriptionStatusQuery(userId));
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        var userId = GetUserId();

        var result = await _mediator.Send(new CancelSubscriptionCommand(userId));
        if (result)
        {
            Message = "Your subscription has been cancelled. You will retain access until the end of your billing period.";
        }
        else
        {
            Message = "Unable to cancel subscription. Please try again or contact support.";
        }

        SubscriptionStatus = await _mediator.Send(new GetSubscriptionStatusQuery(userId));
        return Page();
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? throw new InvalidOperationException("User ID not found.");
    }
}
