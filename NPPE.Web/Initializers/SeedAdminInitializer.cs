using Microsoft.AspNetCore.Identity;
using NPPE.Domain.Entities;

namespace NPPE.Web.Initializers;

public static class SeedAdminInitializer
{
    public async static Task SeedTestUsers(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles
        var adminRole = "Admin";
        var studentRole = "Student";

        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        if (!await roleManager.RoleExistsAsync(studentRole))
            await roleManager.CreateAsync(new IdentityRole(studentRole));

        // Create admin user
        const string adminEmail = "admin@nppe.ca";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "NPPE",
                LastName = "Admin",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(admin, "Admin@123!");
            await userManager.AddToRoleAsync(admin, adminRole);
        }

        // Create student user
        const string studentEmail = "student@nppe.ca";
        if (await userManager.FindByEmailAsync(studentEmail) == null)
        {
            var student = new AppUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "NPPE",
                LastName = "Student",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(student, "Student@123!");
            await userManager.AddToRoleAsync(student, studentRole);
        }
    }
}
