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
}
