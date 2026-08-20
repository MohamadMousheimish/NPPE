using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;

namespace NPPE.E2ETests;

/// <summary>
/// Boots the real app on a real Kestrel socket (so a browser can reach it) while
/// swapping SQL Server for a shared in-memory SQLite database. Uses the documented
/// dual-host WebApplicationFactory pattern (Kestrel for the browser + TestServer the
/// factory manages). Both hosts open their OWN connection to a named, shared-cache
/// in-memory DB, so they see the same data without fighting over one connection's
/// transactions. Seeds a premium student and a takeable exam.
/// </summary>
public class E2EWebAppFactory : WebApplicationFactory<Program>
{
    // A per-run temp file DB: unambiguously shared across every connection and both
    // hosts (in-memory shared-cache does not reliably share DDL across connections here).
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"nppe-e2e-{Guid.NewGuid():N}.db");
    private string ConnectionString => $"Data Source={_dbPath}";
    private IHost? _kestrelHost;

    public const string StudentEmail = "e2e-student@nppe.ca";
    public const string StudentPassword = "E2e@123!";
    public const string AdminEmail = "admin@nppe.ca";     // seeded by the app in Development
    public const string AdminPassword = "Admin@123!";

    public string ServerAddress { get; private set; } = "";
    public string ExamTitle { get; } = "E2E Practice Exam";
    public Guid ExamId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_dummy",
                ["Stripe:PublishableKey"] = "pk_test_dummy",
                ["Stripe:WebhookSecret"] = "whsec_e2e_secret",
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["RateLimiting:AuthPermitLimit"] = "1000000",
                // Both hosts share one DB; we create the schema and seed once in SeedAsync.
                ["Database:SkipStartupInitialization"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.FullName?.Contains("DbContextOptions") ?? false))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(ConnectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the TestServer host (returned so the factory's Services works), then
        // re-target the builder at Kestrel and build a second host on a real port.
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel().UseUrls("http://127.0.0.1:0"));

        // Start Kestrel FIRST and fully (synchronously) so it creates the schema before
        // the TestServer host starts — no two EnsureCreated calls run concurrently.
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        ServerAddress = _kestrelHost.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        testHost.Start();
        return testHost;
    }

    /// <summary>
    /// Runs once (from the fixture) with no host contending: builds the schema, seeds
    /// the app's roles + demo admin/student, then a premium student and a takeable exam.
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Create the schema and the app's standard roles/demo users exactly once.
        await sp.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
        await NPPE.Web.Initializers.SeedAdminInitializer.SeedAsync(sp, isDevelopment: true,
            sp.GetRequiredService<IConfiguration>());

        var users = sp.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(StudentEmail) is null)
        {
            var student = new AppUser
            {
                UserName = StudentEmail,
                Email = StudentEmail,
                EmailConfirmed = true,
                FirstName = "E2E",
                LastName = "Student",
                IsPremium = true
            };
            await users.CreateAsync(student, StudentPassword);
            await users.AddToRoleAsync(student, NppeRoles.Student);
        }

        var db = sp.GetRequiredService<ApplicationDbContext>();
        if (!await db.Exams.AnyAsync(e => e.Title == ExamTitle))
        {
            var exam = new Exam { Title = ExamTitle, Description = "Seeded for E2E.", IsActive = true };
            db.Exams.Add(exam);
            for (var i = 0; i < 3; i++)
            {
                db.Questions.Add(new Question
                {
                    ExamId = exam.Id,
                    Text = $"E2E question {i + 1}?",
                    IsActive = true,
                    ExplanationForCorrect = "Correct.",
                    ExplanationForIncorrect = "Incorrect.",
                    Options = Enumerable.Range(0, 4).Select(o => new AnswerOption
                    {
                        Text = $"Option {(char)('A' + o)}",
                        Label = (char)('A' + o),
                        IsCorrect = o == 0
                    }).ToList()
                });
            }
            await db.SaveChangesAsync();
        }

        ExamId = (await db.Exams.FirstAsync(e => e.Title == ExamTitle)).Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kestrelHost?.Dispose();
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* best effort */ }
        }
    }
}
