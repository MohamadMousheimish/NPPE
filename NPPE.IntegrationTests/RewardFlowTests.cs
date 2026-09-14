using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NPPE.Application.Commands.Rewards.ModerateReward;
using NPPE.Application.Commands.Rewards.RequestCompletionReward;
using NPPE.Application.Services;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;
using NPPE.Infrastructure.Persistence;
using Xunit;

namespace NPPE.IntegrationTests;

public class RewardFlowTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public RewardFlowTests(NppeWebAppFactory factory) => _factory = factory;

    /// <summary>Records the refund it was asked to make instead of calling Stripe.</summary>
    private sealed class RefundStub : IPaymentRefundService
    {
        public long? LastAmountCents;
        public Task<string> RefundBySessionAsync(string sessionId, long amountCents, CancellationToken ct = default)
        {
            LastAmountCents = amountCents;
            return Task.FromResult("re_test_123");
        }
    }

    [Fact]
    public async Task Claim_requires_all_exams_and_a_review_then_admin_refund_issues_5_percent()
    {
        var stub = new RefundStub();
        using var app = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IPaymentRefundService>();
            s.AddSingleton<IPaymentRefundService>(stub);
        }));

        string studentId;
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            studentId = (await users.FindByEmailAsync(WebTest.StudentEmail))!.Id;

            var exam = new Exam { Title = "Reward Exam", Description = "x", IsActive = true };
            db.Exams.Add(exam);
            db.ExamUnlocks.Add(new ExamUnlock { UserId = studentId, Exam = exam, AttemptsAllowed = 2 });
            db.Payments.Add(new Payment
            {
                UserId = studentId, StripeSessionId = "cs_test_reward", Amount = 80m, Currency = "cad",
                Status = NPPE.Domain.Enums.PaymentStatus.Succeeded, PaymentType = NPPE.Domain.Enums.PaymentType.ExamPack,
                ExamsPurchased = 2, PaidAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Finished the exam but no review yet → cannot claim.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exam = db.Exams.First(e => e.Title == "Reward Exam");
            db.ExamAttempts.Add(new ExamAttempt { UserId = studentId, ExamId = exam.Id, Score = 7, TotalQuestions = 10, TakenAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.False(await m.Send(new RequestCompletionRewardCommand(studentId))); // no review yet
        }

        // Leave a review, then claim → a pending reward for 5% of $80 = $4.00.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Feedbacks.Add(new Feedback { UserId = studentId, AuthorName = "Rex W.", Rating = 5, Comment = "Loved it.", IsApproved = false });
            await db.SaveChangesAsync();

            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.True(await m.Send(new RequestCompletionRewardCommand(studentId)));
        }

        Guid rewardId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reward = db.CompletionRewards.Single(r => r.UserId == studentId);
            Assert.Equal(RewardStatus.Requested, reward.Status);
            Assert.Equal(4.00m, reward.RefundAmount);
            rewardId = reward.Id;
        }

        // Admin approves → Stripe refund of 400 cents, status Refunded.
        using (var scope = app.Services.CreateScope())
        {
            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.True(await m.Send(new ApproveCompletionRewardCommand(rewardId, "admin-1")));
        }

        Assert.Equal(400, stub.LastAmountCents);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reward = db.CompletionRewards.Single(r => r.UserId == studentId);
            Assert.Equal(RewardStatus.Refunded, reward.Status);
            Assert.Equal("re_test_123", reward.StripeRefundId);
            Assert.Equal(4.00m, reward.RefundAmount);
        }
    }
}
