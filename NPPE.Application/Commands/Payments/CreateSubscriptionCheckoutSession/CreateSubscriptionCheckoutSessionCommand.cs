using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.CreateSubscriptionCheckoutSession;

public record CreateSubscriptionCheckoutSessionCommand : IRequest<string>
{
    public string UserId { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public class CreateSubscriptionCheckoutSessionCommandHandler
    : IRequestHandler<CreateSubscriptionCheckoutSessionCommand, string>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public CreateSubscriptionCheckoutSessionCommandHandler(
        IPaymentRepository paymentRepository,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<string> Handle(
        CreateSubscriptionCheckoutSessionCommand request, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found.");

        // Get or create Stripe Customer (required for subscriptions)
        string customerId;
        if (!string.IsNullOrEmpty(user.StripeCustomerId))
        {
            customerId = user.StripeCustomerId;
        }
        else
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Metadata = new Dictionary<string, string> { { "user_id", user.Id } }
            }, new RequestOptions { IdempotencyKey = $"customer_{user.Id}" });
            customerId = customer.Id;
            user.StripeCustomerId = customerId;
            await _userManager.UpdateAsync(user);
        }

        // Create pending payment record
        var payment = new Payment
        {
            UserId = request.UserId,
            Amount = PricingPlans.MonthlyPrice,
            Currency = Currencies.Canadian,
            Status = PaymentStatus.Pending,
            PaymentType = PaymentType.Subscription,
        };
        await _paymentRepository.AddAsync(payment);

        // Create Stripe Checkout Session in subscription mode
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currencies.Canadian,
                        UnitAmount = PricingPlans.MonthlyPriceCents,
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month"
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = PricingPlans.MonthlyProductName,
                            Description = PricingPlans.MonthlyProductDescription
                        }
                    },
                    Quantity = 1
                }
            ],
            SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = request.CancelUrl,
            ClientReferenceId = payment.Id.ToString(),
            Metadata = new Dictionary<string, string>
            {
                { "user_id", request.UserId },
                { "payment_type", "subscription" }
            }
        };

        // Idempotency key (keyed on our pending payment id) so a retried request
        // reuses the same Checkout Session instead of creating a duplicate.
        var service = new SessionService();
        var session = await service.CreateAsync(
            options, new RequestOptions { IdempotencyKey = $"checkout_{payment.Id}" });

        payment.StripeSessionId = session.Id;
        await _paymentRepository.UpdateAsync(payment);

        return session.Url;
    }
}
