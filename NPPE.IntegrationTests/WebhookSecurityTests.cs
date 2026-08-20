using System.Net;
using System.Text;

namespace NPPE.IntegrationTests;

public class WebhookSecurityTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public WebhookSecurityTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Webhook_is_reachable_anonymously_but_rejects_missing_signature()
    {
        var client = WebTest.NewClient(_factory);
        var content = new StringContent("{\"type\":\"checkout.session.completed\"}", Encoding.UTF8, "application/json");
        var res = await client.PostAsync("/Payments/Webhook", content);

        // Reachable without auth (not a 302 to login), but forged/unsigned events are
        // rejected before any processing (signature verification throws -> 500).
        Assert.NotEqual(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [Fact]
    public async Task Webhook_rejects_bogus_signature()
    {
        var client = WebTest.NewClient(_factory);
        var content = new StringContent("{\"type\":\"checkout.session.completed\"}", Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "t=123,v1=deadbeef");
        var res = await client.PostAsync("/Payments/Webhook", content);
        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
    }
}
