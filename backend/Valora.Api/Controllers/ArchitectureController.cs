using Microsoft.AspNetCore.Mvc;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class ArchitectureController : ControllerBase
{
    [HttpGet("/admin/architecture/status")]
    public IActionResult Status() => Ok(new { ok = true, dataProvider = "api", legado = "preserved", postgresSchema = "valorapesquisa", allowApiProductionCutover = false });
}
