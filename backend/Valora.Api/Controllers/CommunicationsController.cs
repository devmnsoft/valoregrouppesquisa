using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class CommunicationsController(ICommunicationRepository communications) : ControllerBase
{
    [HttpGet("/communications")]
    public async Task<IActionResult> List() => Ok(new { ok = true, communications = await communications.ListAsync() });

    [HttpPost("/communications/result/{responseId:guid}/send-email")]
    public async Task<IActionResult> SendResultEmail(Guid responseId)
    {
        await communications.AddEmailJobAsync(responseId, "pending-provider@local.invalid", "pending-provider");
        return Accepted(new { ok = true, responseId, emailJobStatus = "pending-provider", message = "Job de e-mail registrado sem bloquear o resultado." });
    }

    [Authorize]
    [HttpGet("/admin/email-jobs")]
    public async Task<IActionResult> AdminEmailJobs() => Ok(new { ok = true, jobs = await communications.ListAsync() });

    [Authorize]
    [HttpPost("/admin/email-jobs/process")]
    public IActionResult ProcessEmailJobs() => Accepted(new { ok = true, status = "processing-delegated", message = "Processamento seguro deve ser executado pelo worker/SMTP configurado; endpoint administrativo registrado para operação." });
}
