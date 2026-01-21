using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.CreateCheckoutSession;

namespace NPPE.Web.Pages.Payments
{
    [Authorize(Roles = "Student")]
    public class CheckoutModel : PageModel
    {
        private readonly IMediator _mediator;

        public CheckoutModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? throw new InvalidOperationException("User ID not found.");

            var successUrl = Url.Page("/Payments/Success", null, null, Request.Scheme)
                             + (ReturnUrl != null ? $"?returnUrl={Uri.EscapeDataString(ReturnUrl)}" : "");
            var cancelUrl = ReturnUrl ?? Url.Page("/Student/Exams/Index", null, null, Request.Scheme);

            var checkoutUrl = await _mediator.Send(new CreateCheckoutSessionCommand
            {
                UserId = userId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl!
            });

            return Redirect(checkoutUrl);
        }
    }
}
