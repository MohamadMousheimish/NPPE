using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Payments.HandlePaymentWebhook;

namespace NPPE.Web.Pages.Payments
{
    [IgnoreAntiforgeryToken]
    public class WebhookModel : PageModel
    {
        private readonly IMediator _mediator;

        public WebhookModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            await _mediator.Send(new HandlePaymentWebhookCommand
            {
                JsonBody = json,
                StripeSignatureHeader = signature!
            });

            return new OkResult(); // Stripe expects 200 OK
        }
    }
}
