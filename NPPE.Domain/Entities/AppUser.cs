using Microsoft.AspNetCore.Identity;

namespace NPPE.Domain.Entities;
public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPremium { get; set; } = false;
    public string? StripeCustomerId { get; set; }
}
