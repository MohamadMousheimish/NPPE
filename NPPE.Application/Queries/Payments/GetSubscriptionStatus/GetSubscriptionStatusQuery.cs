using MediatR;
using Microsoft.AspNetCore.Identity;
using NPPE.Application.DTOs.Payments;
using NPPE.Domain.Entities;

namespace NPPE.Application.Queries.Payments.GetSubscriptionStatus;

public record GetSubscriptionStatusQuery(string UserId) : IRequest<SubscriptionStatusDto?>;

public class GetSubscriptionStatusQueryHandler
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto?>
{
    private readonly UserManager<AppUser> _userManager;

    public GetSubscriptionStatusQueryHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<SubscriptionStatusDto?> Handle(
        GetSubscriptionStatusQuery request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return null;

        return new SubscriptionStatusDto
        {
            IsPremium = user.IsPremium,
            HasSubscription = !string.IsNullOrEmpty(user.StripeSubscriptionId),
            SubscriptionEndDate = user.SubscriptionEndDate,
            IsCancelPending = user.SubscriptionEndDate.HasValue
                && user.SubscriptionEndDate > DateTime.UtcNow
        };
    }
}
