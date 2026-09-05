using MediatR;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Services;
using Stripe;
using Stripe.Checkout;

namespace NPPE.Application.Commands.Payments.ConfirmCheckoutSession;

/// <summary>
/// Confirms a completed Checkout Session on the success page and grants the purchased
/// exam pack immediately, rather than waiting for the asynchronous webhook. The grant is
/// idempotent (claimed once per session), so it's safe if the webhook also fires.
/// </summary>
public record ConfirmCheckoutSessionCommand(string UserId, string SessionId) : IRequest<bool>;

public class ConfirmCheckoutSessionCommandHandler : IRequestHandler<ConfirmCheckoutSessionCommand, bool>
{
    private readonly IConfiguration _configuration;
    private readonly IExamPackGranter _granter;

    public ConfirmCheckoutSessionCommandHandler(IConfiguration configuration, IExamPackGranter granter)
    {
        _configuration = configuration;
        _granter = granter;
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

        // Trust only a paid session that was opened for this signed-in user.
        if (session.PaymentStatus != "paid")
            return false;
        if (session.Metadata == null
            || !session.Metadata.TryGetValue("user_id", out var uid)
            || uid != request.UserId)
            return false;

        await _granter.GrantAsync(session.Id, session.CustomerDetails?.Address?.Country);
        return true;
    }
}
