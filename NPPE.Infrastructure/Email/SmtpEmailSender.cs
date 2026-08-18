using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NPPE.Application.Email;

namespace NPPE.Infrastructure.Email;

/// <summary>
/// Provider-agnostic SMTP sender. Works with any SMTP host — a domain mailbox,
/// Gmail, or the SMTP mode of Resend/SendGrid/Mailgun — configured under the
/// "Email" section. When no host is configured (typical in local dev) it logs the
/// message instead of sending, so flows like password reset still complete without
/// an SMTP account.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Email:Host"];
        var from = _config["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            // Not configured (e.g. local dev): log instead of sending so the caller still succeeds.
            _logger.LogWarning(
                "Email not configured (Email:Host/From missing). Would have sent to {To} — \"{Subject}\".\n{Body}",
                to, subject, htmlBody);
            return;
        }

        var port = int.TryParse(_config["Email:Port"], out var p) ? p : 587;
        var user = _config["Email:User"];
        var password = _config["Email:Password"];
        var fromName = _config["Email:FromName"] ?? "NPPE Prep";
        var useSsl = !bool.TryParse(_config["Email:UseSsl"], out var ssl) || ssl; // default true

        using var client = new SmtpClient(host, port) { EnableSsl = useSsl };
        if (!string.IsNullOrEmpty(user))
            client.Credentials = new NetworkCredential(user, password);

        using var message = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message, ct);
    }
}
