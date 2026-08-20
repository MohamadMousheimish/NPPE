using System.Net;

namespace NPPE.IntegrationTests;

public class SmokeTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public SmokeTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_page_is_public()
    {
        var res = await WebTest.NewClient(_factory).GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Healthz_is_anonymous_and_ok()
    {
        var res = await WebTest.NewClient(_factory).GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Root_requires_authentication()
    {
        var res = await WebTest.NewClient(_factory).GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Seeded_student_can_log_in()
    {
        var res = await WebTest.LoginAsync(WebTest.NewClient(_factory), WebTest.StudentEmail, WebTest.StudentPassword);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    }
}
