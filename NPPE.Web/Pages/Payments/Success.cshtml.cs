using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.ConfirmCheckoutSession;
using NPPE.Application.Repositories;

namespace NPPE.Web.Pages.Payments
{
    [Authorize(Roles = "Student")]
    public class SuccessModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IPaymentRepository _payments;

        public SuccessModel(IMediator mediator, IPaymentRepository payments)
        {
            _mediator = mediator;
            _payments = payments;
        }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        /// <summary>True once the Checkout Session is verified and premium is active.</summary>
        public bool Confirmed { get; private set; }

        // For the GA4 / Google Ads purchase-conversion event.
        public decimal PurchaseValue { get; private set; }
        public string? TransactionId { get; private set; }

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

            if (Confirmed)
            {
                var payment = await _payments.GetBySessionIdAsync(session_id);
                PurchaseValue = payment?.Amount ?? 0m;
                TransactionId = session_id;
            }
        }
    }
}
