using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NPPE.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.ConfirmCheckoutSession;

/// <summary>
/// Confirms a completed Checkout Session on the success page so premium takes
/// effect immediately, rather than waiting for the asynchronous webhook. The
/// webhook remains the authoritative source for payment bookkeeping; this only
/// flips the live <see cref="AppUser.IsPremium"/> flag and is safely idempotent.
/// </summary>
public record ConfirmCheckoutSessionCommand(string UserId, string SessionId) : IRequest<bool>;

public class ConfirmCheckoutSessionCommandHandler : IRequestHandler<ConfirmCheckoutSessionCommand, bool>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public ConfirmCheckoutSessionCommandHandler(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<bool> Handle(ConfirmCheckoutSessionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.SessionId))
            return false;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        Session session;
        try
        {
            session = await new SessionService().GetAsync(request.SessionId, cancellationToken: ct);
        }
        catch (StripeException)
        {
            return false; // unknown or invalid session id
        }

        // Trust only a paid one-time session or a created subscription...
        var isPaid = session.PaymentStatus == "paid";
        var isSubscription = session.Mode == "subscription" && session.SubscriptionId != null;
        if (!isPaid && !isSubscription)
            return false;

        // ...and only when the session was opened for this signed-in user.
        if (session.Metadata == null ||
            !session.Metadata.TryGetValue("user_id", out var uid) ||
            uid != request.UserId)
            return false;

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return false;

        if (!user.IsPremium)
        {
            user.IsPremium = true;
            if (isSubscription)
                user.StripeSubscriptionId ??= session.SubscriptionId;
            await _userManager.UpdateAsync(user);
        }

        return true;
    }
}
