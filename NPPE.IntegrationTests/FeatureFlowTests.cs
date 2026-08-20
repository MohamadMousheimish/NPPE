using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPPE.Infrastructure.Persistence;

namespace NPPE.IntegrationTests;

public class FeatureFlowTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public FeatureFlowTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_can_create_an_exam()
    {
        var admin = await WebTest.LoggedInAsync(_factory, WebTest.AdminEmail, WebTest.AdminPassword);
        var res = await WebTest.PostFormAsync(admin, "/Admin/Exams/Create", "/Admin/Exams/Create", new()
        {
            ["Input.Title"] = "Integration Created Exam",
            ["Input.Description"] = "made by integration test",
            ["Input.IsActive"] = "true"
        });
        // The page re-renders (200) with a success message; the exam is persisted.
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Exams.AnyAsync(e => e.Title == "Integration Created Exam" && e.IsActive));
    }

    [Fact]
    public async Task Admin_can_record_a_cost_and_it_appears_on_finance()
    {
        var admin = await WebTest.LoggedInAsync(_factory, WebTest.AdminEmail, WebTest.AdminPassword);
        var res = await WebTest.PostFormAsync(admin, "/Admin/Finance/RecordCost", "/Admin/Finance/RecordCost", new()
        {
            ["Input.Provider"] = "AzureIntegration",
            ["Input.Category"] = "Hosting",
            ["Input.Amount"] = "156",
            ["Input.IncurredOn"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["Input.IsRecurring"] = "false"
        });
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        var finance = await admin.GetStringAsync("/Admin/Finance?period=all");
        Assert.Contains("AzureIntegration", finance);
        Assert.Contains("156", finance);
    }

    [Fact]
    public async Task Premium_student_can_take_an_exam_and_see_a_scored_result()
    {
        var examId = await _factory.SeedExamAsync("Integration Take Exam", questions: 3);
        await _factory.SetPremiumAsync(WebTest.StudentEmail);

        var student = await WebTest.LoggedInAsync(_factory, WebTest.StudentEmail, WebTest.StudentPassword);

        // Load the exam (premium passes the gate) and answer every question.
        var takeHtml = await student.GetStringAsync($"/Student/Exams/Take?id={examId}");
        var token = Regex.Match(takeHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ExamId"] = examId.ToString()
        };
        var seen = new HashSet<string>();
        foreach (Match m in Regex.Matches(takeHtml, "name=\"(Answers\\[[^\"]+\\])\"[^>]*value=\"([^\"]+)\""))
        {
            var name = m.Groups[1].Value;
            if (seen.Add(name)) form[name] = m.Groups[2].Value; // first option per question
        }
        Assert.Equal(3, seen.Count); // all three questions answered

        var post = await student.PostAsync("/Student/Exams/Take", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Contains("/Student/Exams/Results", post.Headers.Location!.ToString());

        var results = await student.GetAsync(post.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(a => a.ExamId == examId);
        Assert.NotNull(attempt);
        Assert.Equal(3, attempt!.Score); // every first-option is the correct answer, so 3/3
    }
}
