using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NPPE.Application.Commands.Payments.HandlePaymentWebhook;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace NPPE.Tests;

/// <summary>
/// Exercises the webhook reconciliation logic via the internal ProcessEventAsync,
/// so we test the business rules without forging Stripe signatures. Signature
/// verification itself lives in the (thin) public Handle method.
/// </summary>
public class HandlePaymentWebhookCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IProcessedStripeEventRepository> _processedEvents = new();
    private readonly Mock<UserManager<AppUser>> _userManager = MockUserManager();

    public HandlePaymentWebhookCommandHandlerTests()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    private static Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        var mgr = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
        return mgr;
    }

    private HandlePaymentWebhookCommandHandler CreateHandler() =>
        new(_payments.Object, _userManager.Object, new Mock<IConfiguration>().Object,
            _users.Object, _uow.Object, _processedEvents.Object,
            NullLogger<HandlePaymentWebhookCommandHandler>.Instance);

    private static Event Wrap(string type, IHasObject payload, string id = "evt_test") =>
        new() { Id = id, Type = type, Data = new EventData { Object = payload } };

    private static AppUser NewUser(string id = "user_1") =>
        new() { Id = id, Email = "s@nppe.ca", FirstName = "S", LastName = "T", IsPremium = false };

    [Fact]
    public async Task Checkout_completed_subscription_grants_premium_and_stores_subscription()
    {
        var user = NewUser();
        var payment = new Payment { UserId = user.Id, Status = PaymentStatus.Pending, PaymentType = PaymentType.Subscription };
        _payments.Setup(p => p.GetBySessionIdAsync("cs_1")).ReturnsAsync(payment);
        _userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var evt = Wrap("checkout.session.completed", new Session
        {
            Id = "cs_1",
            Mode = "subscription",
            PaymentStatus = "paid",
            SubscriptionId = "sub_1",
            CustomerId = "cus_1",
            Metadata = new Dictionary<string, string> { ["user_id"] = user.Id }
        });

        await CreateHandler().ProcessEventAsync(evt);

        Assert.True(user.IsPremium);
        Assert.Equal("sub_1", user.StripeSubscriptionId);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("sub_1", payment.StripeSubscriptionId);
        _userManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Checkout_completed_one_time_grants_premium_without_subscription()
    {
        var user = NewUser();
        var payment = new Payment { UserId = user.Id, Status = PaymentStatus.Pending, PaymentType = PaymentType.OneTime };
        _payments.Setup(p => p.GetBySessionIdAsync("cs_2")).ReturnsAsync(payment);
        _userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var evt = Wrap("checkout.session.completed", new Session
        {
            Id = "cs_2",
            Mode = "payment",
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string> { ["user_id"] = user.Id }
        });

        await CreateHandler().ProcessEventAsync(evt);

        Assert.True(user.IsPremium);
        Assert.Null(user.StripeSubscriptionId);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public async Task Checkout_completed_is_ignored_when_payment_already_processed()
    {
        var payment = new Payment { UserId = "user_1", Status = PaymentStatus.Succeeded };
        _payments.Setup(p => p.GetBySessionIdAsync("cs_3")).ReturnsAsync(payment);

        var evt = Wrap("checkout.session.completed", new Session
        {
            Id = "cs_3",
            Mode = "payment",
            PaymentStatus = "paid"
        });

        await CreateHandler().ProcessEventAsync(evt);

        // Guard: an already-processed payment must not re-run, so no user lookup/update.
        _userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Subscription_deleted_revokes_premium_when_no_one_time_payment()
    {
        var user = NewUser();
        user.IsPremium = true;
        user.StripeSubscriptionId = "sub_1";
        _users.Setup(u => u.GetByStripeSubscriptionIdAsync("sub_1")).ReturnsAsync(user);
        _payments.Setup(p => p.HasSucceededOneTimePaymentAsync(user.Id)).ReturnsAsync(false);
        _payments.Setup(p => p.GetBySubscriptionIdAsync("sub_1")).ReturnsAsync((Payment?)null);

        await CreateHandler().ProcessEventAsync(
            Wrap("customer.subscription.deleted", new Subscription { Id = "sub_1", CustomerId = "cus_1", Status = "canceled" }));

        Assert.False(user.IsPremium);
        Assert.Null(user.StripeSubscriptionId);
    }

    [Fact]
    public async Task Subscription_deleted_keeps_premium_when_one_time_payment_exists()
    {
        var user = NewUser();
        user.IsPremium = true;
        user.StripeSubscriptionId = "sub_1";
        _users.Setup(u => u.GetByStripeSubscriptionIdAsync("sub_1")).ReturnsAsync(user);
        _payments.Setup(p => p.HasSucceededOneTimePaymentAsync(user.Id)).ReturnsAsync(true);
        _payments.Setup(p => p.GetBySubscriptionIdAsync("sub_1")).ReturnsAsync((Payment?)null);

        await CreateHandler().ProcessEventAsync(
            Wrap("customer.subscription.deleted", new Subscription { Id = "sub_1", CustomerId = "cus_1", Status = "canceled" }));

        Assert.True(user.IsPremium); // retained thanks to the lifetime one-time purchase
        Assert.Null(user.StripeSubscriptionId);
    }

    [Fact]
    public async Task Invoice_payment_succeeded_records_renewal_row_for_subscription_cycle()
    {
        var user = NewUser();
        var original = new Payment { UserId = user.Id, StripeSubscriptionId = "sub_1", PaymentType = PaymentType.Subscription };
        _payments.Setup(p => p.GetBySubscriptionIdAsync("sub_1")).ReturnsAsync(original);
        _users.Setup(u => u.GetByStripeSubscriptionIdAsync("sub_1")).ReturnsAsync(user);

        Payment? added = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>())).Callback<Payment>(p => added = p).Returns(Task.CompletedTask);

        var invoice = new Invoice
        {
            Id = "in_1",
            BillingReason = "subscription_cycle",
            AmountPaid = 999,
            Currency = "cad",
            Parent = new InvoiceParent
            {
                SubscriptionDetails = new InvoiceParentSubscriptionDetails { SubscriptionId = "sub_1" }
            }
        };

        await CreateHandler().ProcessEventAsync(Wrap("invoice.payment_succeeded", invoice));

        Assert.NotNull(added);
        Assert.Equal(9.99m, added!.Amount);
        Assert.Equal(PaymentType.Subscription, added.PaymentType);
        Assert.Equal(PaymentStatus.Succeeded, added.Status);
        Assert.Equal("in_1", added.StripeInvoiceId);
        Assert.True(user.IsPremium);
    }

    [Fact]
    public async Task Invoice_payment_succeeded_does_not_duplicate_on_redelivery()
    {
        var user = NewUser();
        var original = new Payment { UserId = user.Id, StripeSubscriptionId = "sub_1", PaymentType = PaymentType.Subscription };
        _payments.Setup(p => p.GetBySubscriptionIdAsync("sub_1")).ReturnsAsync(original);
        _users.Setup(u => u.GetByStripeSubscriptionIdAsync("sub_1")).ReturnsAsync(user);
        _payments.Setup(p => p.HasPaymentForInvoiceAsync("in_1")).ReturnsAsync(true); // already recorded

        var invoice = new Invoice
        {
            Id = "in_1",
            BillingReason = "subscription_cycle",
            AmountPaid = 999,
            Currency = "cad",
            Parent = new InvoiceParent
            {
                SubscriptionDetails = new InvoiceParentSubscriptionDetails { SubscriptionId = "sub_1" }
            }
        };

        await CreateHandler().ProcessEventAsync(Wrap("invoice.payment_succeeded", invoice));

        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>()), Times.Never); // deduped
    }

    [Fact]
    public async Task Already_processed_event_is_skipped_entirely()
    {
        _processedEvents.Setup(p => p.ExistsAsync("evt_dup")).ReturnsAsync(true);

        var evt = Wrap("checkout.session.completed",
            new Session { Id = "cs_9", Mode = "payment", PaymentStatus = "paid" }, "evt_dup");

        await CreateHandler().ProcessEventAsync(evt);

        // The whole handler body is short-circuited and the event is not re-recorded.
        _payments.Verify(p => p.GetBySessionIdAsync(It.IsAny<string>()), Times.Never);
        _processedEvents.Verify(p => p.AddAsync(It.IsAny<ProcessedStripeEvent>()), Times.Never);
    }
}
