using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.CreateExamPackCheckoutSession;

public record CreateExamPackCheckoutSessionCommand : IRequest<string>
{
    public string UserId { get; init; } = string.Empty;
    public string PackId { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public class CreateExamPackCheckoutSessionCommandHandler
    : IRequestHandler<CreateExamPackCheckoutSessionCommand, string>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public CreateExamPackCheckoutSessionCommandHandler(
        IPaymentRepository paymentRepository,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<string> Handle(CreateExamPackCheckoutSessionCommand request, CancellationToken ct)
    {
        var pack = PricingPlans.GetPack(request.PackId)
            ?? throw new InvalidOperationException($"Unknown exam pack '{request.PackId}'.");

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found.");

        // Pending payment — ExamsPurchased is the source of truth the granter reads.
        var payment = new Payment
        {
            UserId = request.UserId,
            Amount = pack.PriceCents / 100m,
            Currency = Currencies.Canadian,
            Status = PaymentStatus.Pending,
            PaymentType = PaymentType.ExamPack,
            ExamsPurchased = pack.ExamCount
        };
        await _paymentRepository.AddAsync(payment);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = ["card"],
            CustomerEmail = user.Email,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currencies.Canadian,
                        UnitAmount = pack.PriceCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = PricingPlans.PackProductName(pack),
                            Description = PricingPlans.PackProductDescription(pack)
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
                { "payment_type", "exam_pack" },
                { "pack_id", pack.Id },
                { "exam_count", pack.ExamCount.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(
            options, new RequestOptions { IdempotencyKey = $"checkout_{payment.Id}" }, ct);

        payment.StripeSessionId = session.Id;
        await _paymentRepository.UpdateAsync(payment);

        return session.Url;
    }
}
