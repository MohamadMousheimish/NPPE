using System.Net;

namespace NPPE.IntegrationTests;

public class AuthorizationTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public AuthorizationTests(NppeWebAppFactory factory) => _factory = factory;

    public static IEnumerable<object[]> ProtectedPages() => new[]
    {
        new object[] { "/" },
        new object[] { "/Admin/Exams/Index" },
        new object[] { "/Admin/Finance" },
        new object[] { "/Student/Exams/Index" },
        new object[] { "/Payments/Billing" },
        new object[] { "/Account/Profile" },
    };

    public static IEnumerable<object[]> AdminPages() => new[]
    {
        new object[] { "/Admin/Exams/Index" },
        new object[] { "/Admin/Exams/Create" },
        new object[] { "/Admin/Finance" },
        new object[] { "/Admin/Finance/RecordCost" },
    };

    [Theory]
    [MemberData(nameof(ProtectedPages))]
    public async Task Anonymous_is_redirected_to_login(string path)
    {
        var res = await WebTest.NewClient(_factory).GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location!.ToString());
    }

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task Student_is_denied_admin_pages(string path)
    {
        var client = await WebTest.LoggedInAsync(_factory, WebTest.StudentEmail, WebTest.StudentPassword);
        var res = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/AccessDenied", res.Headers.Location!.ToString());
    }

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task Admin_can_access_admin_pages(string path)
    {
        var client = await WebTest.LoggedInAsync(_factory, WebTest.AdminEmail, WebTest.AdminPassword);
        var res = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Student_can_access_student_area()
    {
        var client = await WebTest.LoggedInAsync(_factory, WebTest.StudentEmail, WebTest.StudentPassword);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student/Exams/Index")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Payments/Billing")).StatusCode);
    }

    [Fact]
    public async Task Admin_is_denied_student_only_pages()
    {
        var client = await WebTest.LoggedInAsync(_factory, WebTest.AdminEmail, WebTest.AdminPassword);
        var res = await client.GetAsync("/Payments/Billing"); // [Authorize(Roles = "Student")]
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/AccessDenied", res.Headers.Location!.ToString());
    }
}
