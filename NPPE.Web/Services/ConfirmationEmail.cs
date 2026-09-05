using Microsoft.Extensions.Localization;
using NPPE.Application.Email;
using NPPE.Web.Resources;

namespace NPPE.Web.Services;

/// <summary>
/// Builds and sends the "confirm your email" message. Shared by the register page and
/// the resend flow so the copy and markup stay in one place; the caller supplies the
/// confirmation link (built from its own page/request context).
/// </summary>
internal static class ConfirmationEmail
{
    public static Task SendAsync(
        IEmailSender sender,
        IStringLocalizer<SharedResource> localizer,
        string toEmail,
        string callbackUrl,
        CancellationToken ct = default)
    {
        var subject = localizer["Confirm your email"].Value;
        var body =
            $"<p>{localizer["Thanks for signing up for NPPE Prep. Confirm your email to activate your account."]}</p>" +
            $"<p><a href=\"{callbackUrl}\">{localizer["Confirm my email"]}</a></p>" +
            $"<p>{localizer["If you didn't create this account, you can safely ignore this email."]}</p>";

        return sender.SendEmailAsync(toEmail, subject, body, ct);
    }
}
