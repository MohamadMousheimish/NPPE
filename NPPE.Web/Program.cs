using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPPE.Application.Queries.Exams.GetAllExams;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;
using NPPE.Infrastructure.Persistence.Repositories;
using NPPE.Web.Initializers;
using NPPE.Web.Resources;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging. Reads any "Serilog" section from configuration (so a file,
// Seq, or Application Insights sink can be added without code changes) and always
// writes to the console.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
builder.Services.AddRazorPages()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource)))
                .AddRazorPagesOptions(options =>
                    // Throttle the auth pages (login/register/reset) against brute force.
                    options.Conventions.AddFolderApplicationModelConvention("/Account",
                        model => model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"))));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Set to true later if email confirmation needed
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<NPPE.Web.Localization.LocalizedIdentityErrorDescriber>()
.AddDefaultTokenProviders();

// Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAllExamsQuery).Assembly));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IAnswerOptionRepository, AnswerOptionRepository>();
builder.Services.AddScoped<IExamAttemptRepository, ExamAttemptRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IProcessedStripeEventRepository, ProcessedStripeEventRepository>();
builder.Services.AddScoped<ICostRepository, CostRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<NPPE.Application.Documents.IExamDocumentParser, NPPE.Infrastructure.Documents.ExamDocumentParser>();
builder.Services.AddScoped<NPPE.Application.Email.IEmailSender, NPPE.Infrastructure.Email.SmtpEmailSender>();

// Liveness/readiness probe for the host, including a database connectivity check.
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

// Per-IP rate limiting for the auth pages (partitioned by client IP, which is the
// real caller thanks to the forwarded-headers middleware above).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Honour X-Forwarded-* from the hosting reverse proxy (Azure, Nginx, etc.) so
// Request.Scheme is "https". Stripe return URLs and OAuth redirect URIs are built
// from the scheme, and would otherwise be generated as insecure "http".
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add role-based policies.
// Premium access is intentionally NOT a policy: it is enforced at the point of
// use via the live AppUser.IsPremium flag (see Student/Exams/Take) so that a
// purchase takes effect immediately without requiring the user to re-login.
builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole(NppeRoles.Admin))
                .AddPolicy("StudentOnly", policy => policy.RequireRole(NppeRoles.Student));

var supportedCultures = new[] { "en", "fr" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0])
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

builder.Services.AddLocalization();

var app = builder.Build();

// Must run before any middleware that inspects the scheme/host (HTTPS redirect,
// authentication, URL generation).
app.UseForwardedHeaders();

// Concise per-request log line (method, path, status, elapsed).
app.UseSerilogRequestLogging();

// Security response headers. The CSP allows self-hosted assets, Google Fonts, and
// inline script/style (the app uses inline <script> blocks and style attributes);
// tightening script-src to nonces is a future hardening step.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapHealthChecks("/healthz");

// Persist the chosen language in a cookie so it sticks across navigation.
app.MapGet("/set-culture", (string culture, string? returnUrl, HttpContext ctx) =>
{
    if (supportedCultures.Contains(culture))
    {
        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/" });
    }
    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Seed roles (all environments) and users (demo users in Development, or a
// configuration-provided admin in other environments)
await SeedAdminInitializer.SeedAsync(app);



app.Run();
