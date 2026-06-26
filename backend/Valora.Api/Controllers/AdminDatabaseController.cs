using Microsoft.AspNetCore.Mvc;
using Valora.Infrastructure.Database;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class AdminDatabaseController(IWebHostEnvironment env, MigrationRunner runner) : ControllerBase
{
    [HttpPost("/admin/database/migrate")]
    public async Task<IActionResult> Migrate()
    {
        if (!env.IsDevelopment() && !env.EnvironmentName.Equals("Local", StringComparison.OrdinalIgnoreCase)) return NotFound(new { ok = false });
        var root = Directory.GetCurrentDirectory().Contains("Valora.Api") ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../..")) : Directory.GetCurrentDirectory();
        return Ok(new { ok = true, applied = await runner.RunAsync(root) });
    }
}
