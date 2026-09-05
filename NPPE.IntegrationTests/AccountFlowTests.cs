using System.Net;

namespace NPPE.IntegrationTests;

public class AccountFlowTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public AccountFlowTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Registration_grants_student_role_so_new_user_can_reach_pricing()
    {
        var client = WebTest.NewClient(_factory);
        var register = await WebTest.PostFormAsync(client, "/Account/Register", "/Account/Register", new()
        {
            ["Input.FirstName"] = "New",
            ["Input.LastName"] = "Student",
            ["Input.Email"] = "newstudent@test.ca",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.Redirect, register.StatusCode); // -> /Account/RegisterConfirmation

        // Sign-in now requires a confirmed email; simulate clicking the link.
        await _factory.ConfirmEmailAsync("newstudent@test.ca");

        var authed = await WebTest.LoggedInAsync(_factory, "newstudent@test.ca", "Passw0rd!");

        // The regression we fixed: without the Student role this would be AccessDenied.
        var pricing = await authed.GetAsync("/Payments/Pricing");
        Assert.Equal(HttpStatusCode.OK, pricing.StatusCode);
        var exams = await authed.GetAsync("/Student/Exams/Index");
        Assert.Equal(HttpStatusCode.OK, exams.StatusCode);
    }

    [Fact]
    public async Task Unconfirmed_user_cannot_sign_in()
    {
        var client = WebTest.NewClient(_factory);
        await WebTest.PostFormAsync(client, "/Account/Register", "/Account/Register", new()
        {
            ["Input.FirstName"] = "Unconfirmed",
            ["Input.LastName"] = "User",
            ["Input.Email"] = "unconfirmed@test.ca",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!"
        });

        // Correct credentials, but the email isn't confirmed — sign-in is blocked, so
        // the page re-renders (200) instead of redirecting in.
        var login = await WebTest.LoginAsync(WebTest.NewClient(_factory), "unconfirmed@test.ca", "Passw0rd!");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Registration_with_reserved_company_domain_is_rejected()
    {
        var client = WebTest.NewClient(_factory);
        var res = await WebTest.PostFormAsync(client, "/Account/Register", "/Account/Register", new()
        {
            ["Input.FirstName"] = "Brand",
            ["Input.LastName"] = "Squatter",
            ["Input.Email"] = "someone@nppeacademy.com", // reserved company domain
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!"
        });

        // Re-renders the page with a validation error (200), does not create the account.
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Null(await _factory.FindUserAsync("someone@nppeacademy.com"));
    }

    [Fact]
    public async Task Non_premium_student_is_gated_to_pricing_when_taking_an_exam()
    {
        // The seeded student is not premium in the test DB.
        var client = await WebTest.LoggedInAsync(_factory, WebTest.StudentEmail, WebTest.StudentPassword);
        var res = await client.GetAsync($"/Student/Exams/Take?id={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Payments/Pricing", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Duplicate_registration_is_rejected()
    {
        var client = WebTest.NewClient(_factory);
        var res = await WebTest.PostFormAsync(client, "/Account/Register", "/Account/Register", new()
        {
            ["Input.FirstName"] = "Dupe",
            ["Input.LastName"] = "User",
            ["Input.Email"] = WebTest.StudentEmail, // already seeded
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!"
        });
        // Re-renders the page with a validation error (200), does not redirect to login.
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
