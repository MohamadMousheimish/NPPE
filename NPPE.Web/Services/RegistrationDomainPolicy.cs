namespace NPPE.Web.Services;

/// <summary>
/// Decides whether an email address may self-register. The company's own domain(s)
/// are reserved so the public can't create accounts under our brand — those users are
/// provisioned by the admin (via seed configuration) instead of the public register page.
/// The reserved list is config-driven (<c>Registration:ReservedEmailDomains</c>).
/// </summary>
public class RegistrationDomainPolicy
{
    private readonly HashSet<string> _reserved;

    public RegistrationDomainPolicy(IConfiguration configuration)
    {
        _reserved = (configuration.GetSection("Registration:ReservedEmailDomains").Get<string[]>() ?? Array.Empty<string>())
            .Select(d => d.Trim().TrimStart('@').ToLowerInvariant())
            .Where(d => d.Length > 0)
            .ToHashSet();
    }

    /// <summary>True when the address belongs to a reserved company domain.</summary>
    public bool IsReserved(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return false;

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        return _reserved.Contains(domain);
    }
}
