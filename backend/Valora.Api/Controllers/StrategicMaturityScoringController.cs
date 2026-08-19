using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Results;

namespace Valora.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/scoring/strategic-maturity")]
public sealed class StrategicMaturityScoringController(StrategicMaturityScoringService scoring) : ControllerBase
{
    [HttpPost("preview")]
    public ActionResult<StrategicMaturityResult> Preview(IReadOnlyList<StrategicMaturityAnswer> answers)
    {
        try { return Ok(scoring.Calculate(answers)); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return UnprocessableEntity(new { code = "STRATEGIC_MATURITY_INVALID_ANSWERS", message = exception.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }
}
