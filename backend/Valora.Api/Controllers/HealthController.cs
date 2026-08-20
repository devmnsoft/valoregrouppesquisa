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

    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            using var connection = factory.Create();
            var database = await connection.ExecuteScalarAsync<int>("SELECT 1;") == 1;
            var outboxBacklog = await SafeCountAsync(connection,
                "SELECT count(*)::int FROM valorapesquisa.email_jobs WHERE status IN ('pending','processing') AND is_deleted=false;");
            var failedEmails = await SafeCountAsync(connection,
                "SELECT count(*)::int FROM valorapesquisa.email_jobs WHERE status='failed' AND is_deleted=false;");
            var intelligenceBacklog = await SafeCountAsync(connection,
                "SELECT count(*)::int FROM valorapesquisa.intelligence_processing_jobs WHERE status IN ('pending','processing');");
            var payload = Base(new
            {
                database = database ? "ok" : "fail",
                api = "ok",
                web = "external_probe_required",
                bff = "external_probe_required",
                workers = configuration.GetValue("Valora:Processing:Enabled", true) ? "enabled" : "disabled",
                outbox = new { status = outboxBacklog is null ? "not_available" : "ok", backlog = outboxBacklog, failed = failedEmails },
                storage = Status(configuration["Storage:Provider"]),
                pdf = configuration.GetValue<bool>("Certificates:PdfEnabled") || configuration.GetValue<bool>("Reports:PdfEnabled") ? "configured" : "not_configured",
                intelligenceQueue = new { status = intelligenceBacklog is null ? "not_available" : "ok", backlog = intelligenceBacklog },
                externalServices = configuration.GetSection("ExternalServices").Exists() ? "configured" : "not_configured"
            });
            return database ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Readiness health failed. CorrelationId={CorrelationId}", CorrelationId());
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                Base(new { ok = false, database = "fail", status = "not_ready" }));
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

    [HttpGet("/health/email")]
    public IActionResult Email() => Ok(Base(new
    {
        email = configuration.GetValue<bool>("Email:Enabled")
            ? string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) ? "not_configured" : "configured"
            : "disabled"
    }));

    [HttpGet("/health/storage")]
    public IActionResult Storage() => Ok(Base(new { storage = "local_or_database", backupDirConfigured = !string.IsNullOrWhiteSpace(configuration["VALORA_BACKUP_DIR"]) }));

    [HttpGet("/health/version")]
    public IActionResult Version() => Ok(Base(new { version = VersionValue(), build = configuration["Build:Sha"] ?? "local" }));

    [HttpGet("/health/config")]
    public IActionResult Config()
    {
        var signingKey = configuration["Jwt:SigningKey"];
        var isDemoKey = signingKey?.TrimStart().StartsWith("DEV_ONLY_", StringComparison.OrdinalIgnoreCase) == true;
        var jwtStatus = string.IsNullOrWhiteSpace(signingKey)
            ? "missing"
            : signingKey.Trim().Length < 32 || environment.IsProduction() && isDemoKey
                ? "invalid"
                : "configured";

        return Ok(Base(new
        {
            // Somente estados sanitizados: este endpoint nunca devolve chaves, senhas ou connection strings.
            jwt = new
            {
                signingKey = jwtStatus,
                issuer = Status(configuration["Jwt:Issuer"]),
                audience = Status(configuration["Jwt:Audience"])
            },
            postgres = Status(configuration.GetConnectionString("Postgres") ?? configuration.GetConnectionString("DefaultConnection")),
            postgresConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres") ?? configuration.GetConnectionString("DefaultConnection")),
            email = configuration.GetValue<bool>("Email:Enabled") ? Status(configuration["Email:Smtp:Host"] ?? configuration["Email:SmtpHost"]) : "not_configured",
            emailEnabled = configuration.GetValue<bool>("Email:Enabled"),
            pdf = configuration.GetValue<bool>("Certificates:PdfEnabled") || configuration.GetValue<bool>("Reports:PdfEnabled") ? "configured" : "not_configured",
            storage = Status(configuration["Storage:Provider"]),
            processingEnabled = configuration.GetValue("Valora:Processing:Enabled", true),
            demoSeedEnabled = !environment.IsProduction() &&
                (configuration.GetValue<bool>("Demo:SeedEnabled") ||
                 string.Equals(configuration["VALORA_SEED_DEMO"], "true", StringComparison.OrdinalIgnoreCase))
        }));
    }

    private object Base(object extra)
    {
        var basePayload = new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["service"] = "Valora.Api",
            ["environment"] = environment.EnvironmentName,
            ["version"] = VersionValue(),
            ["correlationId"] = CorrelationId(),
            ["timestamp"] = DateTimeOffset.UtcNow
        };
        foreach (var p in extra.GetType().GetProperties()) basePayload[p.Name] = p.GetValue(extra);
        return basePayload;
    }

    private object MigrationInfo() => new { lastApplied = string.Empty, pendingCount = 0 };
    private static string Status(string? value) => string.IsNullOrWhiteSpace(value) ? "missing" : "configured";
    private string VersionValue() => typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    private string CorrelationId() => HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

    private static async Task<int?> SafeCountAsync(System.Data.IDbConnection connection, string sql)
    {
        try { return await connection.ExecuteScalarAsync<int>(sql); }
        catch { return null; }
    }
}
