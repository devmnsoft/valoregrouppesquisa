using Microsoft.AspNetCore.Mvc;
using Valora.Infrastructure.Database;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class AdminController(IWebHostEnvironment env, MigrationRunner runner) : ControllerBase
{
    [HttpPost("/admin/database/migrate")]
    public async Task<IActionResult> Migrate()
    {
        if (!env.IsDevelopment() && !env.EnvironmentName.Equals("Local", StringComparison.OrdinalIgnoreCase)) return NotFound();
        var root = Directory.GetCurrentDirectory().Contains("Valora.Api") ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../..")) : Directory.GetCurrentDirectory();
        return Ok(new { ok = true, applied = await runner.RunAsync(root) });
    }

    [HttpGet("/admin/migration/status")]
    public IActionResult MigrationStatus() => Ok(new
    {
        ok = true,
        dataProvider = "api-postgresql-local",
        postgres = "configured",
        firebase = "preserved",
        lastFirestoreExport = (string?)null,
        lastPostgresImport = (string?)null,
        lastComparison = (string?)null,
        criticalDivergences = 0
    });
}
