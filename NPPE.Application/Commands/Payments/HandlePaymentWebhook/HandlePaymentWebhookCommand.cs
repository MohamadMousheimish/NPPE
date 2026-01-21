using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.HandlePaymentWebhook;

public record HandlePaymentWebhookCommand : IRequest<Unit>
{
    public string JsonBody { get; init; } = string.Empty;
    public string StripeSignatureHeader { get; init; } = string.Empty;
}

public class HandlePaymentWebhookCommandHandler : IRequestHandler<HandlePaymentWebhookCommand, Unit>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public HandlePaymentWebhookCommandHandler(IPaymentRepository paymentRepository, UserManager<AppUser> userManager, IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<Unit> Handle(HandlePaymentWebhookCommand request, CancellationToken ct)
    {
        // Validate webhook signature
        var json = request.JsonBody;
        var stripeSignatureHeader = request.StripeSignatureHeader;
        var secret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, secret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session?.PaymentStatus == "paid")
                {
                    // Find payment by session ID
                    var payment = await _paymentRepository.GetBySessionIdAsync(session.Id);
                    if (payment != null && payment.Status == PaymentStatus.Pending)
                    {
                        // Mark payment as succeeded
                        payment.Status = PaymentStatus.Succeeded;
                        payment.PaidAt = DateTime.UtcNow;
                        await _paymentRepository.UpdateAsync(payment);

                        // Mark user as premium
                        var userId = session.Metadata?["user_id"] ?? payment.UserId;
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            user.IsPremium = true;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error (in production, use ILogger)
            Console.WriteLine($"Webhook error: {ex.Message}");
            throw;
        }

        return Unit.Value;
    }
}