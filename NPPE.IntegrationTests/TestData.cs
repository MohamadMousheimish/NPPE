using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;

namespace NPPE.IntegrationTests;

/// <summary>Seeds/mutates data directly via the app's services (for arranging integration tests).</summary>
public static class TestData
{
    public static async Task<Guid> SeedExamAsync(this NppeWebAppFactory factory, string title, int questions = 3)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exam = new Exam { Title = title, Description = "seeded", IsActive = true };
        db.Exams.Add(exam);

        for (var i = 0; i < questions; i++)
        {
            var q = new Question
            {
                ExamId = exam.Id,
                Text = $"Question {i + 1}?",
                IsActive = true,
                ExplanationForCorrect = "Correct.",
                ExplanationForIncorrect = "Incorrect.",
                Options = Enumerable.Range(0, 4).Select(o => new AnswerOption
                {
                    Text = $"Option {(char)('A' + o)}",
                    Label = (char)('A' + o),
                    IsCorrect = o == 0
                }).ToList()
            };
            db.Questions.Add(q);
        }

        await db.SaveChangesAsync();
        return exam.Id;
    }

    /// <summary>Looks up a user by email (null if not created).</summary>
    public static async Task<AppUser?> FindUserAsync(this NppeWebAppFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return await users.FindByEmailAsync(email);
    }

    /// <summary>Confirms a user's email (mirrors clicking the confirmation link).</summary>
    public static async Task ConfirmEmailAsync(this NppeWebAppFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User {email} not found.");

        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await users.ConfirmEmailAsync(user, token);
    }

    /// <summary>Unlocks a specific exam for a user (mirrors what buying an exam pack does).</summary>
    public static async Task UnlockExamAsync(this NppeWebAppFactory factory, string email, Guid examId)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await users.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User {email} not found.");

        db.ExamUnlocks.Add(new ExamUnlock { UserId = user.Id, ExamId = examId });
        await db.SaveChangesAsync();
    }
}
