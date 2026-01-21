using MediatR;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.CreateCheckoutSession;
public record CreateCheckoutSessionCommand : IRequest<string>
{
    public string UserId { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, string>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;

    public CreateCheckoutSessionCommandHandler(IPaymentRepository paymentRepository, IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _configuration = configuration;
    }

    public async Task<string> Handle(CreateCheckoutSessionCommand request, CancellationToken ct)
    {
        // Configure Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        // Create payment record (pending)
        var payment = new Payment
        {
            UserId = request.UserId,
            Amount = 29m, // CAD $29
            Currency = Currencies.Canadian,
            Status = PaymentStatus.Pending,
        };
        await _paymentRepository.AddAsync(payment);

        // Create Stripe Checkout Session
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currencies.Canadian,
                        UnitAmount = 2900, // $29.00 → in cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "NPPE Exam Prep Full Access",
                            Description = "One-time payment for unlimited exam attempts"
                        }
                    },
                    Quantity = 1
                }
            ],
            SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = request.CancelUrl,
            ClientReferenceId = payment.Id.ToString(), // Link to our payment record
            Metadata = new Dictionary<string, string>
            {
                { "user_id", request.UserId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        // Update payment with session ID
        payment.StripeSessionId = session.Id;
        await _paymentRepository.UpdateAsync(payment);

        return session.Url; // Redirect URL
    }
}