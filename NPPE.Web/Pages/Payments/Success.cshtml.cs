using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.ConfirmCheckoutSession;

namespace NPPE.Web.Pages.Payments
{
    [Authorize(Roles = "Student")]
    public class SuccessModel : PageModel
    {
        private readonly IMediator _mediator;

        public SuccessModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        /// <summary>True once the Checkout Session is verified and premium is active.</summary>
        public bool Confirmed { get; private set; }

        public async Task OnGetAsync(string? session_id)
        {
            if (string.IsNullOrEmpty(session_id))
                return;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return;

            // Verify with Stripe and flip premium immediately (the webhook is the
            // authoritative backstop and reconciles the payment record shortly after).
            Confirmed = await _mediator.Send(new ConfirmCheckoutSessionCommand(userId, session_id));
        }
    }
}
