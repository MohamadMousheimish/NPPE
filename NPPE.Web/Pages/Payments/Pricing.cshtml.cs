using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.CreateCheckoutSession;
using NPPE.Application.Commands.Payments.CreateSubscriptionCheckoutSession;

namespace NPPE.Web.Pages.Payments;

[Authorize(Roles = "Student")]
public class PricingModel : PageModel
{
    private readonly IMediator _mediator;

    public PricingModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string planType)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? throw new InvalidOperationException("User ID not found.");

        var successUrl = Url.Page("/Payments/Success", null, null, Request.Scheme)!;
        var cancelUrl = Url.Page("/Payments/Cancel", null, null, Request.Scheme)!;

        string checkoutUrl;

        if (planType == "subscription")
        {
            checkoutUrl = await _mediator.Send(new CreateSubscriptionCheckoutSessionCommand
            {
                UserId = userId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            });
        }
        else
        {
            checkoutUrl = await _mediator.Send(new CreateCheckoutSessionCommand
            {
                UserId = userId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            });
        }

        return Redirect(checkoutUrl);
    }
}
