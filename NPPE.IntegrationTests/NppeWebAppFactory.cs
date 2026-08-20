using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NPPE.Infrastructure.Persistence;

namespace NPPE.IntegrationTests;

/// <summary>
/// Hosts the real app in-memory for integration tests, swapping SQL Server for a
/// shared in-memory SQLite database (schema built from the model via EnsureCreated).
/// Runs in the Development environment so the demo admin/student users + roles seed.
/// </summary>
public class NppeWebAppFactory : WebApplicationFactory<Program>
{
    public const string WebhookSecret = "whsec_integration_test_secret";

    // A single kept-open connection makes the in-memory SQLite DB live for the whole factory.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_dummy",
                ["Stripe:PublishableKey"] = "pk_test_dummy",
                ["Stripe:WebhookSecret"] = WebhookSecret,
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:", // ignored (DbContext overridden)
                ["RateLimiting:AuthPermitLimit"] = "1000000", // effectively disable the limiter in tests
                // Run without external auth, exactly like CI (no Google secrets configured).
                ["Authentication:Google:ClientId"] = "",
                ["Authentication:Google:ClientSecret"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration: the context, its options, AND the
            // options-configuration (which holds the UseSqlServer call). Leaving the
            // latter causes a dual-provider conflict.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.FullName?.Contains("DbContextOptions") ?? false))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            _connection.Open();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
