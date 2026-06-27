using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Api.Controllers;

[ApiController]
[Authorize]
public sealed class MigrationController : ControllerBase
{
    [HttpGet("/admin/migration/status")]
    public IActionResult Status() => Ok(new { ok = true, dataProvider = "api-postgresql-local", postgresSchema = "valorapesquisa", firebase = "preserved" });
}
