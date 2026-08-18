namespace NPPE.Application.Email;

/// <summary>
/// Sends transactional emails (password reset, and later email confirmation).
/// Implemented in the Infrastructure layer so the delivery mechanism (SMTP,
/// provider API) stays out of the application core.
/// </summary>
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
