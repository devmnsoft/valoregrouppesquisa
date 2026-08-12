using Dapper;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;

namespace Valora.Api.Controllers;

/// <summary>Local bootstrap verification. This route deliberately does not exist outside Development.</summary>
[ApiController]
public sealed class DevelopmentAuthDiagnosticsController(
    IWebHostEnvironment environment,
    IDbConnectionFactory connections,
    IPasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet("/dev/auth/diagnostics")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Get()
    {
        if (!environment.IsDevelopment()) return NotFound();

        try
        {
            using var connection = connections.Create();
            const string sql = """
                SELECT u.id, u.status, u.deleted_at, u.password_hash,
                       EXISTS(SELECT 1 FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id
                              WHERE ur.user_id=u.id AND r.code='admin_valora' AND r.deleted_at IS NULL) AS has_admin_role,
                       (SELECT count(*) FROM valorapesquisa.user_roles ur JOIN valorapesquisa.role_permissions rp ON rp.role_id=ur.role_id
                         WHERE ur.user_id=u.id) AS permission_count,
                       p.code AS plan_code, s.status AS subscription_status
                FROM valorapesquisa.users u
                LEFT JOIN valorapesquisa.subscriptions s ON s.organization_id=u.organization_id AND s.status='active'
                    AND s.deleted_at IS NULL AND (s.ends_at IS NULL OR s.ends_at>now())
                LEFT JOIN valorapesquisa.plans p ON p.id=s.plan_id
                WHERE lower(u.email)='e2e-admin@valoragroup.local'
                ORDER BY u.deleted_at NULLS FIRST LIMIT 1
                """;
            var row = await connection.QuerySingleOrDefaultAsync(sql);
            bool passwordValid = VerifyDevelopmentPassword(row is null ? null : (string?)row.password_hash);
            return Ok(new
            {
                api = "online",
                database = "online",
                superadminExists = row is not null,
                userStatus = row is null ? "missing" : (string)row.status,
                userDeleted = row is not null && row.deleted_at is not null,
                adminValoraRole = row is not null && (bool)row.has_admin_role,
                linkedPermissions = row is null ? 0 : (long)row.permission_count,
                activeSubscription = row is not null && (string?)row.subscription_status == "active",
                plan = row is null ? null : (string?)row.plan_code,
                developmentPasswordVerification = passwordValid ? "valid" : "invalid"
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { api = "online", database = "offline" });
        }
    }

    private bool VerifyDevelopmentPassword(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 60 || !hash.StartsWith("$2", StringComparison.Ordinal)) return false;
        try { return passwordHasher.Verify("Valora!12345", hash); }
        catch (ArgumentException) { return false; }
        catch (FormatException) { return false; }
    }
}
