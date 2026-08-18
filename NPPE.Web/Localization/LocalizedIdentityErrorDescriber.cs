using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using NPPE.Web.Resources;

namespace NPPE.Web.Localization;

/// <summary>
/// Translates ASP.NET Core Identity's built-in error messages (password rules,
/// wrong current password, invalid reset token, etc.) via the shared resource.
/// </summary>
public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _l;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer) => _l = localizer;

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = _l["Passwords must be at least {0} characters.", length] };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = _l["Passwords must have at least one uppercase letter."] };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = _l["Passwords must have at least one lowercase letter."] };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = _l["Passwords must have at least one digit."] };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = _l["Passwords must have at least one non-alphanumeric character."] };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = _l["Passwords must use at least {0} different characters.", uniqueChars] };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = _l["Incorrect password."] };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = _l["Email '{0}' is already taken.", email] };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = _l["Username '{0}' is already taken.", userName] };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = _l["Invalid or expired token."] };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = _l["Email is invalid."] };
}
