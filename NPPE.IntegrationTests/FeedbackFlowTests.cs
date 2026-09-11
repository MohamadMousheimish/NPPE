using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NPPE.Application.Commands.Feedback.ModerateFeedback;
using NPPE.Application.Commands.Feedback.SubmitFeedback;
using NPPE.Application.Queries.Feedback.GetAllFeedback;
using NPPE.Application.Queries.Feedback.GetApprovedFeedback;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;
using Xunit;

namespace NPPE.IntegrationTests;

public class FeedbackFlowTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public FeedbackFlowTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Review_flow_eligibility_moderation_and_public_listing()
    {
        // --- Ineligible: a fresh student with no attempts cannot submit ---
        string freshId;
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var u = new AppUser { UserName = "noattempt@test.ca", Email = "noattempt@test.ca", FirstName = "No", LastName = "Attempt", EmailConfirmed = true };
            await users.CreateAsync(u, "Passw0rd!");
            freshId = u.Id;
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.False(await m.Send(new SubmitFeedbackCommand(freshId, 4, "should be rejected")));
        }

        // --- Eligible: the seeded student, once they have an attempt, can submit ---
        string studentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            studentId = (await users.FindByEmailAsync(WebTest.StudentEmail))!.Id;
            var exam = new Exam { Title = "Feedback Exam", Description = "x", IsActive = true };
            db.Exams.Add(exam);
            db.ExamAttempts.Add(new ExamAttempt { UserId = studentId, Exam = exam, Score = 5, TotalQuestions = 10, TakenAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.True(await m.Send(new SubmitFeedbackCommand(studentId, 5, "Genuinely helped me pass.")));

            // Pending → not public yet
            var approved = await m.Send(new GetApprovedFeedbackQuery(50));
            Assert.DoesNotContain(approved, f => f.Comment == "Genuinely helped me pass.");

            // Approve it
            var all = await m.Send(new GetAllFeedbackQuery());
            var mine = all.First(f => f.Comment == "Genuinely helped me pass.");
            Assert.True(mine.FromStudent);
            await m.Send(new SetFeedbackApprovalCommand(mine.Id, true));

            // Now public, with a privacy-friendly author name
            approved = await m.Send(new GetApprovedFeedbackQuery(50));
            var pub = Assert.Single(approved, f => f.Comment == "Genuinely helped me pass.");
            Assert.Equal(5, pub.Rating);
            Assert.Equal("NPPE S.", pub.AuthorName); // seeded student is "NPPE Student"
        }

        // --- Admin can seed a testimonial that shows publicly ---
        using (var scope = _factory.Services.CreateScope())
        {
            var m = scope.ServiceProvider.GetRequiredService<IMediator>();
            await m.Send(new UpsertAdminFeedbackCommand(null, "Ada L.", "EIT · Ontario", 5, "A seeded testimonial.", Approved: true));
            var approved = await m.Send(new GetApprovedFeedbackQuery(50));
            Assert.Contains(approved, f => f.AuthorName == "Ada L." && f.Comment == "A seeded testimonial.");
        }
    }
}
