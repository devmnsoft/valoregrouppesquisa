using Dapper;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Contracts;
using Valora.Application.Contracts.Services;

namespace Valora.Api.Controllers;

[ApiController]
[Route("e2e")]
public sealed class E2eFixtureController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IDbConnectionFactory connections,
    IPasswordHasher passwordHasher,
    ILogger<E2eFixtureController> logger) : ControllerBase
{
    private const string AdminEmail = "e2e-admin@valoragroup.local";
    private const string AdminPassword = "Valora!12345";
    private const string PublicToken = "e2e-public-token-sprint32";

    [HttpGet("fixture")]
    public async Task<IActionResult> GetFixture(CancellationToken cancellationToken)
    {
        if (!IsAllowed()) return NotFound(new { ok = false, error = "E2E_FIXTURE_DISABLED", correlationId = HttpContext.TraceIdentifier });
        try
        {
            await EnsurePasswordHashAsync();
            using var db = connections.Create();
            const string sql = @"
SELECT o.id AS OrganizationId, s.id AS SurveyId, f.id AS FormId, COALESCE(sub.plan_id, o.plan_id) AS PlanId
FROM valorapesquisa.organizations o
JOIN valorapesquisa.forms f ON f.organization_id=o.id AND f.name='Diagnóstico Valora Insight E2E'
JOIN valorapesquisa.surveys s ON s.organization_id=o.id AND s.form_id=f.id AND s.status='active'
LEFT JOIN valorapesquisa.subscriptions sub ON sub.organization_id=o.id AND sub.status='active'
WHERE o.slug='valora-e2e-organization'
ORDER BY sub.created_at DESC NULLS LAST
LIMIT 1";
            var fixture = await db.QuerySingleOrDefaultAsync(sql);
            if (fixture is null) return StatusCode(503, new { ok = false, error = "E2E_FIXTURE_NOT_SEEDED", correlationId = HttpContext.TraceIdentifier });
            var row = (IDictionary<string, object>)fixture;
            var organizationId = (Guid)row["organizationid"];
            var surveyId = (Guid)row["surveyid"];
            var formId = (Guid)row["formid"];
            var planId = Convert.ToString(row["planid"]) ?? "free";
            logger.LogInformation("E2E fixture metadata served. OrganizationId={OrganizationId} SurveyId={SurveyId}", organizationId, surveyId);
            return Ok(new
            {
                ok = true,
                organizationId,
                adminEmail = AdminEmail,
                adminPasswordForTestOnly = AdminPassword,
                surveyId,
                publicToken = PublicToken,
                formId,
                planId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load E2E fixture metadata.");
            return StatusCode(500, new { ok = false, error = "E2E_FIXTURE_ERROR", correlationId = HttpContext.TraceIdentifier });
        }
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetFixture(CancellationToken cancellationToken)
    {
        if (!IsAllowed()) return NotFound(new { ok = false, error = "E2E_FIXTURE_DISABLED", correlationId = HttpContext.TraceIdentifier });
        try
        {
            await EnsurePasswordHashAsync();
            using var db = connections.Create();
            await db.ExecuteAsync("UPDATE valorapesquisa.usage_monthly SET metric_value=0, updated_at=now() WHERE organization_id='11111111-1111-1111-1111-111111111111' AND metric_key='responses';");
            logger.LogInformation("E2E fixture reset executed for deterministic local validation.");
            return Ok(new { ok = true, correlationId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset E2E fixture.");
            return StatusCode(500, new { ok = false, error = "E2E_FIXTURE_RESET_ERROR", correlationId = HttpContext.TraceIdentifier });
        }
    }

    private bool IsAllowed()
    {
        if (environment.IsProduction()) return false;
        return environment.IsDevelopment()
            || string.Equals(environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environment.EnvironmentName, "Test", StringComparison.OrdinalIgnoreCase)
            || configuration.GetValue<bool>("E2E:EnableFixtureEndpoints");
    }

    private async Task EnsurePasswordHashAsync()
    {
        using var db = connections.Create();
        var hash = passwordHasher.Hash(AdminPassword);
        await db.ExecuteAsync("UPDATE valorapesquisa.users SET password_hash=@hash, updated_at=now() WHERE lower(email)=lower(@email)", new { hash, email = AdminEmail });
    }
}
