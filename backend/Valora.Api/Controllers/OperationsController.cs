using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Valora.Application.Communication;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class OperationsController(ICommunicationRepository communications, EmailQueueProcessor processor, IOptions<EmailOptions> emailOptions, ILogger<OperationsController> logger) : ControllerBase
{
    [Authorize]
    [HttpGet("/admin/operations/health")]
    public IActionResult Health() => Ok(new { ok = true, api = true, database = "not_checked", csp = true, firebaseLegacy = true, valoraWeb = true, correlationId = HttpContext.TraceIdentifier });

    [Authorize]
    [HttpGet("/admin/operations/email")]
    public async Task<IActionResult> Email()
    {
        var cfg = EmailConfigurationValidator.Validate(emailOptions.Value);
        var jobs = await communications.ListEmailJobsAsync();
        return Ok(new { ok = true, smtp = cfg.CanSend, queue = true, pending = jobs.Count(x => ((object)x).ToString()?.Contains("pending") == true), failed = jobs.Count(x => ((object)x).ToString()?.Contains("failed") == true), lastSent = "sanitized", lastFailure = cfg.Errors.FirstOrDefault() ?? string.Empty, correlationId = HttpContext.TraceIdentifier });
    }

    [Authorize]
    [HttpGet("/admin/operations/free-survey")]
    public IActionResult FreeSurvey() => Ok(new { ok = true, publicLink = true, antiAbuse = true, correlationId = HttpContext.TraceIdentifier });

    [Authorize]
    [HttpGet("/admin/operations/certificates")]
    public IActionResult Certificates() => Ok(new { ok = true, publicValidation = true, certificatePublicEndpoint = "/certificates/validate/{validationCode}", correlationId = HttpContext.TraceIdentifier });

    [Authorize]
    [HttpPost("/admin/operations/email/process-queue")]
    public async Task<IActionResult> ProcessQueue(CancellationToken ct) => Accepted(await processor.ProcessAsync(10, ct));

    [Authorize]
    [HttpPost("/admin/operations/free-survey/repair-link")]
    public IActionResult RepairLink() { logger.LogInformation("Free survey repair link requested. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier); return Accepted(new { ok = true, status = "repair-requested", correlationId = HttpContext.TraceIdentifier }); }
}
