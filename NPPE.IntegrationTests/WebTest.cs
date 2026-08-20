using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NPPE.IntegrationTests;

/// <summary>Shared HTTP helpers for the integration tests (antiforgery, login).</summary>
public static class WebTest
{
    public static HttpClient NewClient(NppeWebAppFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<string> AntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!m.Success) throw new InvalidOperationException($"Antiforgery token not found on {path}.");
        return m.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string path, string tokenPath, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await AntiforgeryTokenAsync(client, tokenPath);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        PostFormAsync(client, "/Account/Login", "/Account/Login", new()
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password
        });

    /// <summary>A client already authenticated as the seeded student / admin.</summary>
    public static async Task<HttpClient> LoggedInAsync(NppeWebAppFactory factory, string email, string password)
    {
        var client = NewClient(factory);
        var res = await LoginAsync(client, email, password);
        if (res.StatusCode != System.Net.HttpStatusCode.Redirect)
            throw new InvalidOperationException($"Login failed for {email}: {res.StatusCode}");
        return client;
    }

    public const string StudentEmail = "student@nppe.ca";
    public const string StudentPassword = "Student@123!";
    public const string AdminEmail = "admin@nppe.ca";
    public const string AdminPassword = "Admin@123!";
}
