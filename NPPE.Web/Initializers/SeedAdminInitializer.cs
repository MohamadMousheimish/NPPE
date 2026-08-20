using Microsoft.AspNetCore.Identity;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;

namespace NPPE.Web.Initializers;

public static class SeedAdminInitializer
{
    public static Task SeedAsync(WebApplication app) =>
        SeedAsync(app.Services, app.Environment.IsDevelopment(), app.Configuration);

    /// <summary>
    /// Seeds roles (all environments) plus demo users (development) or a configured
    /// admin (otherwise). Overload usable outside the WebApplication (e.g. E2E tests).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Roles are required in every environment for authorization to work.
        await EnsureRolesAsync(roleManager);

        if (isDevelopment)
        {
            // Convenient demo accounts for local development only. These use
            // well-known passwords, so they must never be seeded in production.
            await SeedDevUsersAsync(userManager);
        }
        else
        {
            // In non-development environments an initial admin is only created
            // when explicit credentials are supplied via configuration/secrets.
            // No default/backdoor admin is ever created.
            await SeedConfiguredAdminAsync(userManager, configuration);
        }
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { NppeRoles.Admin, NppeRoles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedDevUsersAsync(UserManager<AppUser> userManager)
    {
        await CreateUserIfMissingAsync(userManager, "admin@nppe.ca", "NPPE", "Admin", "Admin@123!", NppeRoles.Admin);
        await CreateUserIfMissingAsync(userManager, "student@nppe.ca", "NPPE", "Student", "Student@123!", NppeRoles.Student);
    }

    private static async Task SeedConfiguredAdminAsync(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        // Nothing configured -> do not create any admin.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var firstName = configuration["SeedAdmin:FirstName"] ?? "NPPE";
        var lastName = configuration["SeedAdmin:LastName"] ?? "Admin";

        await CreateUserIfMissingAsync(userManager, email, firstName, lastName, password, NppeRoles.Admin);
    }

    private static async Task CreateUserIfMissingAsync(
        UserManager<AppUser> userManager,
        string email,
        string firstName,
        string lastName,
        string password,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) != null)
            return;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
