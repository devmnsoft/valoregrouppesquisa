using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class PlanRepository(IDbConnectionFactory connections, ILogger<PlanRepository> logger) : IPlanRepository
{
    public async Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        const string sql = """
            SELECT
                id AS Id,
                code AS Code,
                name AS Name,
                is_public AS IsPublic,
                is_active AS IsActive
            FROM valorapesquisa.plans
            WHERE is_public = true
              AND is_active = true
            ORDER BY name;
            """;
        try
        {
            var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
            var plans = (await connection.QueryAsync<PlanRecord>(command)).AsList();
            var result = new List<PlanDto>(plans.Count);
            foreach (var plan in plans) result.Add(await HydrateAsync(connection, plan, cancellationToken));
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to list public plans.");
            throw;
        }
    }

    public async Task<PlanDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        using var connection = connections.Create();
        const string sql = """
            SELECT
                id AS Id,
                code AS Code,
                name AS Name,
                is_public AS IsPublic,
                is_active AS IsActive
            FROM valorapesquisa.plans
            WHERE lower(code) = lower(@PlanCode)
              AND is_active = true
            LIMIT 1;
            """;
        var command = new CommandDefinition(sql, new { PlanCode = id.Trim() }, cancellationToken: cancellationToken);
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRecord>(command);
        return plan is null ? null : await HydrateAsync(connection, plan, cancellationToken);
    }

    private async Task<PlanDto> HydrateAsync(IDbConnection connection, PlanRecord plan, CancellationToken cancellationToken)
    {
        const string limitsSql = """
            SELECT
                id AS Id,
                limit_key AS LimitKey,
                limit_value AS LimitValue,
                period AS Period,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM valorapesquisa.plan_limits
            WHERE plan_id = @PlanId
            ORDER BY COALESCE(updated_at, created_at), id;
            """;
        const string capabilitiesSql = """
            SELECT
                id AS Id,
                capability_key AS CapabilityKey,
                enabled AS Enabled,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM valorapesquisa.plan_capabilities
            WHERE plan_id = @PlanId
            ORDER BY COALESCE(updated_at, created_at), id;
            """;

        var parameters = new { PlanId = plan.Id };
        var limitRows = (await connection.QueryAsync<PlanLimitRecord>(
            new CommandDefinition(limitsSql, parameters, cancellationToken: cancellationToken))).AsList();
        var capabilityRows = (await connection.QueryAsync<PlanCapabilityRecord>(
            new CommandDefinition(capabilitiesSql, parameters, cancellationToken: cancellationToken))).AsList();

        var limits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in limitRows)
        {
            var key = row.LimitKey?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                logger.LogWarning("Ignoring invalid plan limit row with empty key. PlanId={PlanId} PlanCode={PlanCode} RowId={RowId}", plan.Id, plan.Code, row.Id);
                continue;
            }
            if (limits.ContainsKey(key))
                logger.LogWarning("Duplicate plan limit resolved deterministically using the most recently updated row. PlanId={PlanId} PlanCode={PlanCode} LimitKey={LimitKey} RowId={RowId}", plan.Id, plan.Code, key, row.Id);
            limits[key] = row.LimitValue ?? -1; // The canonical contract represents unlimited as NULL.
        }

        var capabilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in capabilityRows)
        {
            var key = row.CapabilityKey?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                logger.LogWarning("Ignoring invalid plan capability row with empty key. PlanId={PlanId} PlanCode={PlanCode} RowId={RowId}", plan.Id, plan.Code, row.Id);
                continue;
            }
            if (capabilities.ContainsKey(key))
                logger.LogWarning("Duplicate plan capability resolved deterministically using the most recently updated row. PlanId={PlanId} PlanCode={PlanCode} CapabilityKey={CapabilityKey} RowId={RowId}", plan.Id, plan.Code, key, row.Id);
            capabilities[key] = row.Enabled ? "enabled" : "disabled";
        }

        if (limitRows.Count > 0 && limits.Count == 0)
            logger.LogError("Plan limits contain no usable keys. PlanId={PlanId} PlanCode={PlanCode}", plan.Id, plan.Code);
        if (capabilityRows.Count > 0 && capabilities.Count == 0)
            logger.LogError("Plan capabilities contain no usable keys. PlanId={PlanId} PlanCode={PlanCode}", plan.Id, plan.Code);

        var displayOrder = plan.Code switch { "free" => 10, "professional" => 20, "enterprise" => 30, _ => 100 };
        var badge = plan.Code switch { "free" => "Para começar", "professional" => "Mais escolhido", "enterprise" => "Sob medida", _ => "Plano" };
        return new PlanDto(plan.Code, plan.Name, badge, plan.Code == "free" ? "Grátis" : "Sob consulta", null, displayOrder, limits, capabilities);
    }

    public async Task<string?> GetCurrentPlanIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        const string sql = """
            SELECT p.code
            FROM valorapesquisa.subscriptions AS s
            INNER JOIN valorapesquisa.plans AS p ON p.id = s.plan_id
            WHERE s.organization_id = @OrganizationId
              AND s.status = 'active'
              AND s.deleted_at IS NULL
              AND p.is_active = true
            ORDER BY s.created_at DESC
            LIMIT 1;
            """;
        return await connection.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: cancellationToken));
    }

    public async Task CreateSubscriptionAsync(Guid organizationId, string planId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);
        using var connection = connections.Create();
        const string sql = """
            INSERT INTO valorapesquisa.subscriptions (organization_id, plan_id, status)
            SELECT @OrganizationId, id, 'active'
            FROM valorapesquisa.plans
            WHERE lower(code) = lower(@PlanCode)
              AND is_active = true
            ON CONFLICT (organization_id) WHERE deleted_at IS NULL
            DO UPDATE SET plan_id = EXCLUDED.plan_id, status = 'active', updated_at = now();
            """;
        await connection.ExecuteAsync(new CommandDefinition(sql, new { OrganizationId = organizationId, PlanCode = planId.Trim() }, cancellationToken: cancellationToken));
    }
}
