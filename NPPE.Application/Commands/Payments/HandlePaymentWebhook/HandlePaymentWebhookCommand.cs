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
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HandlePaymentWebhookCommandHandler(
        IPaymentRepository paymentRepository,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _userManager = userManager;
        _configuration = configuration;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(HandlePaymentWebhookCommand request, CancellationToken ct)
    {
        var json = request.JsonBody;
        var stripeSignatureHeader = request.StripeSignatureHeader;
        var secret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, secret);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook error: {ex.Message}");
            throw;
        }

        return Unit.Value;
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        // For subscription mode, PaymentStatus may be "unpaid" initially
        // but the subscription is still created
        var isPaid = session.PaymentStatus == "paid";
        var isSubscription = session.Mode == "subscription";

        if (!isPaid && !isSubscription) return;

        var payment = await _paymentRepository.GetBySessionIdAsync(session.Id);
        if (payment == null || payment.Status != PaymentStatus.Pending) return;

        // Mark payment as succeeded
        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;

        // Get user
        var userId = session.Metadata?["user_id"] ?? payment.UserId;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        if (isSubscription && session.SubscriptionId != null)
        {
            // Subscription payment: store subscription ID
            payment.StripeSubscriptionId = session.SubscriptionId;
            payment.SubscriptionStatus = SubscriptionStatus.Active;

            user.StripeSubscriptionId = session.SubscriptionId;
            user.StripeCustomerId ??= session.CustomerId;
        }

        user.IsPremium = true;

        // Commit payment + user together: a partial failure here would otherwise
        // leave a paid user without premium, and the "already not Pending" guard
        // above would prevent Stripe retries from ever fixing it.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _paymentRepository.UpdateAsync(payment);
            await _userManager.UpdateAsync(user);
        });
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await _userRepository.GetByStripeCustomerIdAsync(subscription.CustomerId);
        if (user == null) return;

        var payment = await _paymentRepository.GetBySubscriptionIdAsync(subscription.Id);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (subscription.CancelAtPeriodEnd)
            {
                user.SubscriptionEndDate = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                await _userManager.UpdateAsync(user);
            }

            if (payment != null)
            {
                payment.SubscriptionStatus = subscription.Status switch
                {
                    "active" => SubscriptionStatus.Active,
                    "past_due" => SubscriptionStatus.PastDue,
                    "canceled" => SubscriptionStatus.Canceled,
                    _ => payment.SubscriptionStatus
                };
                await _paymentRepository.UpdateAsync(payment);
            }
        });
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await _userRepository.GetByStripeSubscriptionIdAsync(subscription.Id);
        if (user == null) return;

        // Only revoke premium if user has no successful one-time payment
        var hasOneTimePayment = await _paymentRepository.HasSucceededOneTimePaymentAsync(user.Id);
        if (!hasOneTimePayment)
        {
            user.IsPremium = false;
        }

        user.StripeSubscriptionId = null;
        user.SubscriptionEndDate = null;

        var payment = await _paymentRepository.GetBySubscriptionIdAsync(subscription.Id);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _userManager.UpdateAsync(user);
            if (payment != null)
            {
                payment.SubscriptionStatus = SubscriptionStatus.Expired;
                await _paymentRepository.UpdateAsync(payment);
            }
        });
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId == null) return;

        var payment = await _paymentRepository.GetBySubscriptionIdAsync(subscriptionId);
        if (payment != null)
        {
            payment.SubscriptionStatus = SubscriptionStatus.PastDue;
            await _paymentRepository.UpdateAsync(payment);
        }
    }
}
