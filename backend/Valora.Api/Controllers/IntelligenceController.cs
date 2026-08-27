using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Intelligence;

namespace Valora.Api.Controllers;

[Authorize, ApiController, Route("api/v1/intelligence-core")]
public sealed class IntelligenceController(IIntelligenceAnalysisService intelligence) : ControllerBase
{
    [HttpGet("analysis")]
    public async Task<IActionResult> Analysis(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("organization_id"), out var organizationId))
            return UnprocessableEntity(new { code = "ORGANIZATION_CONTEXT_REQUIRED", message = "Selecione uma organização para consultar a leitura de inteligência." });
        return Ok(await intelligence.AnalyzeAsync(organizationId, cancellationToken));
    }
}
