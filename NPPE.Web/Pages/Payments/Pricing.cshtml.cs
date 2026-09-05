using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.CreateExamPackCheckoutSession;

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

    public async Task<IActionResult> OnPostAsync(string packId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? throw new InvalidOperationException("User ID not found.");

        var successUrl = Url.Page("/Payments/Success", null, null, Request.Scheme)!;
        var cancelUrl = Url.Page("/Payments/Cancel", null, null, Request.Scheme)!;

        var checkoutUrl = await _mediator.Send(new CreateExamPackCheckoutSessionCommand
        {
            UserId = userId,
            PackId = packId,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        });

        return Redirect(checkoutUrl);
    }
}
