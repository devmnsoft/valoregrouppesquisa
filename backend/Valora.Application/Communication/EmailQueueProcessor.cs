using Valora.Application.Contracts;
using Valora.Application.Security;
using Microsoft.Extensions.Logging;

namespace Valora.Application.Communication;

public sealed class EmailQueueProcessor(ICommunicationRepository communications, IEmailSender sender, ILogger<EmailQueueProcessor> logger)
{
    public async Task<object> ProcessAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        var jobs = await communications.GetPendingEmailJobsAsync(Math.Clamp(batchSize, 1, 100));
        var sent = 0; var failed = 0;
        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guid id = (Guid)job.id; string to = (string)job.to_email; string subject = (string)job.subject; string body = (string)job.body;
            try { await communications.MarkProcessingAsync(id); await communications.IncrementAttemptsAsync(id); await sender.SendAsync(to, subject, body, cancellationToken); await communications.MarkSentAsync(id); sent++; logger.LogInformation("Email job sent. JobId={JobId} Recipient={Recipient}", id, LogSanitizer.MaskEmail(to)); }
            catch (Exception ex) { failed++; var error = LogSanitizer.SanitizeError(ex) ?? "send-error"; await communications.MarkFailedAsync(id, error); logger.LogWarning(ex, "Email job failed. JobId={JobId} Recipient={Recipient} Error={Error}", id, LogSanitizer.MaskEmail(to), error); }
        }
        return new { ok = true, processed = sent + failed, sent, failed };
    }
}
