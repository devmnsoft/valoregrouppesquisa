using Dapper;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Middleware;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

[ApiController]
public sealed class HealthController(
    IDbConnectionFactory factory,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(Base(new { database = "not_checked", logging = "ok", migration = MigrationInfo() }));

    [HttpGet("/health/database")]
    public async Task<IActionResult> Database()
    {
        try
        {
            using var connection = factory.Create();
            var isHealthy = await connection.ExecuteScalarAsync<int>("SELECT 1;") == 1;
            logger.LogInformation("Health database checked. Healthy={Healthy} CorrelationId={CorrelationId}", isHealthy, CorrelationId());
            return Ok(Base(new { database = isHealthy ? "ok" : "fail" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health database failed. CorrelationId={CorrelationId}", CorrelationId());
            return StatusCode(StatusCodes.Status503ServiceUnavailable, Base(new { ok = false, database = "fail" }));
        }
    }

    [HttpGet("/health/logging")]
    public IActionResult Logging()
    {
        logger.LogInformation("Health logging checked. CorrelationId={CorrelationId}", CorrelationId());
        return Ok(Base(new { logging = "ok" }));
    }

    [HttpGet("/health/migration")]
    public IActionResult Migration() => Ok(Base(new { migration = MigrationInfo() }));

    [HttpGet("/health/version")]
    public IActionResult Version() => Ok(Base(new { version = VersionValue() }));

    [HttpGet("/health/config")]
    public IActionResult Config() => Ok(Base(new { postgresConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")) }));

    private object Base(object extra)
    {
        var basePayload = new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["service"] = "Valora.Api",
            ["environment"] = environment.EnvironmentName,
            ["version"] = VersionValue(),
            ["correlationId"] = CorrelationId(),
            ["time"] = DateTimeOffset.UtcNow
        };
        foreach (var p in extra.GetType().GetProperties()) basePayload[p.Name] = p.GetValue(extra);
        return basePayload;
    }

    private object MigrationInfo() => new { lastApplied = string.Empty, pendingCount = 0 };
    private string VersionValue() => typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    private string CorrelationId() => HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
