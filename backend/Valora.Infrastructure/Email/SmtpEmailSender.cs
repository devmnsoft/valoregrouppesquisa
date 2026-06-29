using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Communication;
using Valora.Application.Contracts;
using Valora.Application.Security;

namespace Valora.Infrastructure.Email;

public sealed class SmtpEmailSender(ILogger<SmtpEmailSender> logger, IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        var status = EmailConfigurationValidator.Validate(cfg);
        if (!status.CanSend)
        {
            logger.LogWarning("SMTP email blocked by configuration. Recipient={Recipient} Errors={Errors}", LogSanitizer.MaskEmail(to), string.Join(";", status.Errors));
            throw new InvalidOperationException("Email SMTP configuration is not ready.");
        }
        if (!EmailConfigurationValidator.IsEmail(to)) throw new ArgumentException("Recipient email is invalid.", nameof(to));
        using var message = new MailMessage { From = new MailAddress(cfg.FromEmail, cfg.FromName), Subject = subject ?? string.Empty, Body = body ?? string.Empty, IsBodyHtml = false };
        message.To.Add(new MailAddress(to));
        if (!string.IsNullOrWhiteSpace(cfg.ReplyTo)) message.ReplyToList.Add(new MailAddress(cfg.ReplyTo));
        using var client = new SmtpClient(cfg.Smtp.Host, cfg.Smtp.Port)
        {
            EnableSsl = cfg.Smtp.UseSsl,
            Timeout = Math.Max(1, cfg.Smtp.TimeoutSeconds) * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(cfg.Smtp.Username, cfg.Smtp.Password)
        };
        try
        {
            logger.LogInformation("SMTP email send started. Recipient={Recipient} SubjectLength={SubjectLength}", LogSanitizer.MaskEmail(to), subject?.Length ?? 0);
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("SMTP email sent. Recipient={Recipient}", LogSanitizer.MaskEmail(to));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP email send failed. Recipient={Recipient} Error={Error}", LogSanitizer.MaskEmail(to), LogSanitizer.SanitizeError(ex));
            throw;
        }
    }
}
