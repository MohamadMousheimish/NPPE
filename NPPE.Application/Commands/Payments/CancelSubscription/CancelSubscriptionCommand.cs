using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;

namespace NPPE.Application.Commands.Payments.CancelSubscription;

public record CancelSubscriptionCommand(string UserId) : IRequest<bool>;

public class CancelSubscriptionCommandHandler
    : IRequestHandler<CancelSubscriptionCommand, bool>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;

    public CancelSubscriptionCommandHandler(
        UserManager<AppUser> userManager,
        IPaymentRepository paymentRepository,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _paymentRepository = paymentRepository;
        _configuration = configuration;
    }

    public async Task<bool> Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || string.IsNullOrEmpty(user.StripeSubscriptionId))
            return false;

        var service = new SubscriptionService();
        var subscription = await service.UpdateAsync(user.StripeSubscriptionId,
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });

        user.SubscriptionEndDate = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        await _userManager.UpdateAsync(user);

        var payment = await _paymentRepository.GetBySubscriptionIdAsync(user.StripeSubscriptionId);
        if (payment != null)
        {
            payment.SubscriptionStatus = SubscriptionStatus.Canceled;
            await _paymentRepository.UpdateAsync(payment);
        }

        return true;
    }
}
