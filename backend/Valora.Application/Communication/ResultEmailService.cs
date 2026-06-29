using Valora.Application.Contracts;
using Valora.Application.DTOs.Communication;
using Valora.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Valora.Application.Communication;

public sealed class ResultEmailService(ICommunicationRepository communications, IEmailSender sender, IOptions<EmailOptions> options, ILogger<ResultEmailService> logger)
{
    public async Task<EmailSendResult> SendResultAsync(Guid responseId, SendResultEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (!EmailConfigurationValidator.IsEmail(request.To)) return new(false, null, "failed-validation", "Destinatário inválido.");
        if (string.IsNullOrWhiteSpace(request.ResultToken)) return new(false, null, "failed-token", "Token do resultado obrigatório.");
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? "Seu resultado Valora Pulse" : request.Subject.Trim();
        var body = EmailTemplateService.BuildResultTemplate(responseId, request.Message, request.IncludeCertificate);
        var jobId = await communications.AddEmailJobAsync(null, responseId, request.To.Trim(), subject, "result-ready", "pending", System.Text.Json.JsonSerializer.Serialize(new { responseId, body }));
        var cfg = EmailConfigurationValidator.Validate(options.Value);
        if (!cfg.CanSend) { await communications.MarkFailedAsync(jobId, "EmailConfigurationInvalid", "failed-config"); return new(true, jobId, "failed-config", "Job criado; SMTP não configurado para envio imediato."); }
        try { await communications.MarkProcessingAsync(jobId); await communications.IncrementAttemptsAsync(jobId); await sender.SendAsync(request.To, subject, body, cancellationToken); await communications.MarkSentAsync(jobId); return new(true, jobId, "sent", "Resultado enviado por e-mail."); }
        catch(Exception ex){ var err=LogSanitizer.SanitizeError(ex) ?? "send-error"; await communications.MarkFailedAsync(jobId, err); logger.LogWarning(ex,"Result email failed. ResponseId={ResponseId} Recipient={Recipient} Error={Error}", responseId, LogSanitizer.MaskEmail(request.To), err); return new(true, jobId, "failed", "Job criado; falha controlada no envio."); }
    }
}
