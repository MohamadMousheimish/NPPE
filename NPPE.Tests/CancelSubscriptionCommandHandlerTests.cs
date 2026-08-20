using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using NPPE.Application.Commands.Payments.CancelSubscription;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using Xunit;

namespace NPPE.Tests;

/// <summary>
/// The cancel handler talks to Stripe over the network for a real subscription, so we
/// pin the guard rails that must short-circuit BEFORE any Stripe call is attempted:
/// an unknown user or a user with no subscription returns false and touches nothing.
/// </summary>
public class CancelSubscriptionCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _payments = new();

    private static Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private CancelSubscriptionCommandHandler CreateHandler(Mock<UserManager<AppUser>> users) =>
        new(users.Object, _payments.Object, new Mock<IConfiguration>().Object);

    [Fact]
    public async Task Unknown_user_returns_false_without_touching_payments()
    {
        var users = MockUserManager();
        users.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var result = await CreateHandler(users).Handle(new CancelSubscriptionCommand("nope"), default);

        Assert.False(result);
        _payments.Verify(p => p.GetBySubscriptionIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task User_without_a_subscription_returns_false()
    {
        var users = MockUserManager();
        users.Setup(m => m.FindByIdAsync("u1"))
            .ReturnsAsync(new AppUser { Id = "u1", StripeSubscriptionId = null });

        var result = await CreateHandler(users).Handle(new CancelSubscriptionCommand("u1"), default);

        Assert.False(result);
        _payments.Verify(p => p.GetBySubscriptionIdAsync(It.IsAny<string>()), Times.Never);
    }
}
