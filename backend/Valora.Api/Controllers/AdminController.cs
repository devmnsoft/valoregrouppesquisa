using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Infrastructure.Database;
namespace Valora.Api.Controllers;
[ApiController]
public sealed class AdminController(IWebHostEnvironment env, IConfiguration config, MigrationRunner runner, ILogger<AdminController> logger) : ControllerBase
{
    private bool LegacyEnabled => env.IsDevelopment() || config.GetValue<bool>("Compatibility:EnableLegacyAdminRoutes");
    [Obsolete("Use official migration operations/checklists. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpPost("/legacy/admin/database/migrate")]
    public async Task<IActionResult> Migrate(){ if(!LegacyEnabled) return NotFound(new { ok=false, error="legacy_route_disabled"}); try{var root=Directory.GetCurrentDirectory().Contains("Valora.Api")?Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),"../..")):Directory.GetCurrentDirectory(); var applied=await runner.RunAsync(root); logger.LogWarning("Legacy migration route executed. Applied={Applied}", applied); return Ok(new{ok=true,applied});}catch(Exception ex){logger.LogError(ex,"Legacy migration route failed."); throw;}}
    [Obsolete("Use /health/version and production readiness documents. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpGet("/legacy/admin/architecture/status")]
    public IActionResult ArchitectureStatus(){ if(!LegacyEnabled) return NotFound(new { ok=false, error="legacy_route_disabled"}); logger.LogWarning("Legacy architecture status route used."); return Ok(new{ok=true,environment=env.EnvironmentName,cutoverAllowed=config.GetValue<bool>("ALLOW_API_PRODUCTION_CUTOVER")});}
    [Obsolete("Use /health/migration. Legacy route is disabled outside Development unless explicitly configured.")]
    [HttpGet("/legacy/admin/migration/status")]
    public IActionResult MigrationStatus(){ if(!LegacyEnabled) return NotFound(new { ok=false, error="legacy_route_disabled"}); logger.LogWarning("Legacy migration status route used."); return Ok(new{ok=true,environment=env.EnvironmentName});}
}
