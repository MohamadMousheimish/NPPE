using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPPE.Application.Services;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;
using Xunit;

namespace NPPE.IntegrationTests;

public class InactiveExamCleanupTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public InactiveExamCleanupTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Removes_only_long_inactive_exams_without_attempts_and_cascades()
    {
        var now = DateTime.UtcNow;
        Guid staleId, staleWithQId, recentId, activeId, attemptedId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var studentId = (await users.FindByEmailAsync(WebTest.StudentEmail))!.Id;

            Exam Mk(string title, bool active, DateTime? deactivatedAt) =>
                new() { Title = title, Description = "x", IsActive = active, DeactivatedAt = deactivatedAt };

            var stale = Mk("Cleanup stale", false, now.AddDays(-20));
            var staleWithQ = Mk("Cleanup stale w/ questions", false, now.AddDays(-20));
            var recent = Mk("Cleanup recent", false, now.AddDays(-3));   // within retention
            var active = Mk("Cleanup active", true, null);
            var attempted = Mk("Cleanup attempted", false, now.AddDays(-40)); // old but has an attempt

            db.Exams.AddRange(stale, staleWithQ, recent, active, attempted);

            staleWithQ.Questions.Add(new Question
            {
                Text = "Q?", IsActive = true, ExplanationForCorrect = "c", ExplanationForIncorrect = "i",
                Options = new List<AnswerOption> { new() { Text = "A", Label = 'A', IsCorrect = true } }
            });
            db.ExamAttempts.Add(new ExamAttempt
            {
                UserId = studentId, Exam = attempted, Score = 1, TotalQuestions = 1, TakenAt = now.AddDays(-40)
            });
            await db.SaveChangesAsync();

            staleId = stale.Id; staleWithQId = staleWithQ.Id; recentId = recent.Id;
            activeId = active.Id; attemptedId = attempted.Id;
        }

        // Act — run the cleaner with a 14-day retention.
        int removed;
        using (var scope = _factory.Services.CreateScope())
        {
            var cleaner = scope.ServiceProvider.GetRequiredService<IInactiveExamCleaner>();
            removed = await cleaner.RemoveExpiredAsync(now, retentionDays: 14);
        }

        Assert.Equal(2, removed);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null(await db.Exams.FindAsync(staleId));
            Assert.Null(await db.Exams.FindAsync(staleWithQId));
            Assert.False(await db.Questions.AnyAsync(q => q.ExamId == staleWithQId)); // cascaded
            Assert.NotNull(await db.Exams.FindAsync(recentId));    // within retention
            Assert.NotNull(await db.Exams.FindAsync(activeId));    // still active
            Assert.NotNull(await db.Exams.FindAsync(attemptedId)); // preserved (has an attempt)
        }
    }
}
