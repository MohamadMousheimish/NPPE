using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace NPPE.E2ETests;

/// <summary>
/// Real-browser journeys against a live Kestrel instance: the student takes an exam
/// end to end, and the admin creates one. These exercise the rendered UI, client
/// validation, antiforgery, and the full server round-trip.
/// </summary>
public class EndToEndTests : PageTest, IClassFixture<E2EFixture>
{
    private readonly E2EWebAppFactory _factory;
    public EndToEndTests(E2EFixture fixture) => _factory = fixture.Factory;

    private string Url(string path) => $"{_factory.ServerAddress.TrimEnd('/')}{path}";

    private async Task LoginAsync(string email, string password)
    {
        await Page.GotoAsync(Url("/Account/Login"));
        await Page.FillAsync("#Input_Email", email);
        await Page.FillAsync("#Input_Password", password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        // Land somewhere that is not the login page.
        await Page.WaitForURLAsync(new Regex(@"^(?!.*/Account/Login).*$"));
    }

    [Fact]
    public async Task Student_can_log_in_take_an_exam_and_see_results()
    {
        await LoginAsync(E2EWebAppFactory.StudentEmail, E2EWebAppFactory.StudentPassword);

        await Page.GotoAsync(Url($"/Student/Exams/Take?id={_factory.ExamId}"));
        var cards = Page.Locator(".q-card");
        var count = await cards.CountAsync();
        Assert.Equal(3, count);

        for (var i = 0; i < count; i++)
            await cards.Nth(i).Locator("input[type=radio]").First.CheckAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit exam" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/Student/Exams/Results"));
    }

    [Fact]
    public async Task Admin_can_log_in_and_create_an_exam()
    {
        await LoginAsync(E2EWebAppFactory.AdminEmail, E2EWebAppFactory.AdminPassword);

        await Page.GotoAsync(Url("/Admin/Exams/Create"));
        await Page.FillAsync("#Input_Title", "E2E Browser Created Exam");
        await Page.FillAsync("#Input_Description", "Created through a real browser.");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create exam" }).ClickAsync();

        // The page re-renders with a success alert.
        await Expect(Page.Locator(".alert--ok")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Anonymous_visiting_a_protected_page_is_sent_to_login()
    {
        await Page.GotoAsync(Url("/Student/Exams/Index"));
        await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
    }
}
