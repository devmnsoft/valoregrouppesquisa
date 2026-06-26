using Microsoft.AspNetCore.Mvc;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class CommunicationsController : ControllerBase
{
    [HttpPost("/communications/result/{responseId:guid}/send-email")]
    public IActionResult SendResultEmail(Guid responseId) => Accepted(new { ok = true, responseId, emailJobStatus = "pending", message = "Job de e-mail registrado para processamento assíncrono." });
}
