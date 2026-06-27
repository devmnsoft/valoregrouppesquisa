using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Email;

public sealed class SmtpEmailSender(ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("SMTP email send requested. Recipient={Recipient} SubjectLength={SubjectLength}", Valora.Application.Security.LogSanitizer.MaskEmail(to), subject?.Length ?? 0);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP email send failed. Recipient={Recipient}", Valora.Application.Security.LogSanitizer.MaskEmail(to));
            throw;
        }
    }
}
