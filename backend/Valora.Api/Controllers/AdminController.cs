using Microsoft.AspNetCore.Mvc;
using Valora.Infrastructure.Database;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class AdminController(IWebHostEnvironment env, MigrationRunner runner) : ControllerBase
{
    [HttpPost("/legacy/admin/database/migrate")]
    public async Task<IActionResult> Migrate()
    {
        if (!env.IsDevelopment() && !env.EnvironmentName.Equals("Local", StringComparison.OrdinalIgnoreCase)) return NotFound();
        var root = Directory.GetCurrentDirectory().Contains("Valora.Api") ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../..")) : Directory.GetCurrentDirectory();
        return Ok(new { ok = true, applied = await runner.RunAsync(root) });
    }



    [HttpGet("/legacy/admin/architecture/status")]
    public IActionResult ArchitectureStatus() => Ok(new
    {
        ok = true,
        dataProvider = "api",
        hybridPrimaryProvider = "firebase",
        allowApiProductionCutover = false,
        apiBaseUrl = "http://localhost:5000",
        postgres = "configured",
        firebase = "preserved",
        gateway = "preserved",
        cloudFunctions = "preserved",
        warnings = new[] { "API/PostgreSQL somente para ambiente local/controlado até cutover aprovado." }
    });

    [HttpGet("/legacy/admin/migration/status")]
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
