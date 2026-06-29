using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Valora.Application.Communication;
using Valora.Application.Contracts;
using Valora.Application.DTOs.Communication;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class CommunicationsController(ICommunicationRepository communications, ResultEmailService resultEmail, EmailQueueProcessor processor, IEmailSender sender, IOptions<EmailOptions> emailOptions) : ControllerBase
{
    [HttpGet("/communications")]
    public async Task<IActionResult> List() => Ok(new { ok = true, communications = await communications.ListAsync() });

    [HttpPost("/communications/result/{responseId:guid}/send-email")]
    public async Task<IActionResult> SendResultEmail(Guid responseId, [FromBody] SendResultEmailRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ResultToken) && User.Identity?.IsAuthenticated != true) return Unauthorized(new { ok = false, code = "result-token-required", message = "ResultToken válido ou usuário autenticado é obrigatório." });
        var result = await resultEmail.SendResultAsync(responseId, request, ct);
        return Accepted(result);
    }

    [Obsolete("Compatibilidade temporária: use POST /communications/result/{responseId}/send-email.")]
    [HttpPost("/communication/result/send")]
    public async Task<IActionResult> SendResultEmailCompat([FromBody] LegacySendResultEmailRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.ResponseId, out var responseId)) return BadRequest(new { ok = false, code = "invalid-response-id" });
        var result = await resultEmail.SendResultAsync(responseId, new SendResultEmailRequest(request.To ?? string.Empty, request.Subject, request.Message, true, request.ResultToken), ct);
        return Accepted(result);
    }

    [Authorize]
    [HttpPost("/communications/email/send")]
    public async Task<IActionResult> SendGenericEmail([FromBody] SendGenericEmailRequest request, CancellationToken ct)
    {
        if (!EmailConfigurationValidator.IsEmail(request.To) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 20000) return BadRequest(new { ok = false, code = "invalid-email-payload" });
        await sender.SendAsync(request.To, request.Subject, request.Body, ct);
        return Accepted(new EmailSendResult(true, null, "sent", "E-mail enviado."));
    }

    [Authorize]
    [HttpGet("/admin/email-jobs")]
    public async Task<IActionResult> AdminEmailJobs([FromQuery] string? status) => Ok(new { ok = true, jobs = await communications.ListEmailJobsAsync(status) });

    [Authorize]
    [HttpPost("/admin/email-jobs/process")]
    public async Task<IActionResult> ProcessEmailJobs([FromBody] ProcessEmailJobsRequest? request, CancellationToken ct) => Accepted(await processor.ProcessAsync(request?.BatchSize ?? 10, ct));

    [Authorize]
    [HttpGet("/admin/email/config/status")]
    public IActionResult EmailConfigStatus() => Ok(EmailConfigurationValidator.Validate(emailOptions.Value));

    [Authorize]
    [HttpGet("/admin/email/deliverability/status")]
    public IActionResult EmailDeliverabilityStatus()
    {
        var cfg = EmailConfigurationValidator.Validate(emailOptions.Value);
        return Ok(new { ok = true, fromEmailConfigured = cfg.FromEmailConfigured, smtpConfigured = cfg.SmtpHostConfigured && cfg.SmtpUserConfigured && cfg.SmtpPasswordConfigured, spfDocumented = true, dkimDocumented = true, dmarcDocumented = true, canSend = cfg.CanSend });
    }

    public sealed record LegacySendResultEmailRequest(string ResponseId, string? ResultToken, string? To, string? Subject, string? Message);
    public sealed record ProcessEmailJobsRequest(int BatchSize);
}
