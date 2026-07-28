using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class PlanRepository(IDbConnectionFactory connections, ILogger<PlanRepository> logger) : IPlanRepository
{
    public async Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync()
    {
        try
        {
            using var connection = connections.Create();
            var plans = await connection.QueryAsync<PlanRecord>("""
                SELECT id AS Id, code AS Code, name AS Name, is_public AS IsPublic, is_active AS IsActive
                FROM valorapesquisa.plans
                WHERE is_public = true AND is_active = true
                ORDER BY name
                """);
            var result = new List<PlanDto>();
            foreach (var plan in plans) result.Add(await HydrateAsync(plan));
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao listar planos públicos.");
            throw;
        }
    }

    public async Task<PlanDto?> GetByIdAsync(string id)
    {
        using var connection = connections.Create();
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRecord>("""
            SELECT id AS Id, code AS Code, name AS Name, is_public AS IsPublic, is_active AS IsActive
            FROM valorapesquisa.plans WHERE code = @id
            """, new { id });
        return plan is null ? null : await HydrateAsync(plan);
    }

    private async Task<PlanDto> HydrateAsync(PlanRecord plan)
    {
        using var connection = connections.Create();
        var limits = (await connection.QueryAsync<PlanLimitRecord>("""
            SELECT limit_key AS LimitKey, limit_value AS LimitValue, period AS Period
            FROM valorapesquisa.plan_limits WHERE plan_id = @planId
            """, new { planId = plan.Id })).ToDictionary(x => x.LimitKey, x => x.LimitValue ?? -1);
        var capabilities = (await connection.QueryAsync<PlanCapabilityRecord>("""
            SELECT capability_key AS CapabilityKey, enabled AS Enabled
            FROM valorapesquisa.plan_capabilities WHERE plan_id = @planId
            """, new { planId = plan.Id })).ToDictionary(x => x.CapabilityKey, x => x.Enabled ? "enabled" : "disabled");
        var displayOrder = plan.Code switch { "free" => 10, "professional" => 20, "enterprise" => 30, _ => 100 };
        var badge = plan.Code switch { "free" => "Para começar", "professional" => "Mais escolhido", "enterprise" => "Sob medida", _ => "Plano" };
        return new PlanDto(plan.Code, plan.Name, badge, plan.Code == "free" ? "Grátis" : "Sob consulta", null, displayOrder, limits, capabilities);
    }

    public async Task<string?> GetCurrentPlanIdAsync(Guid organizationId)
    {
        using var connection = connections.Create();
        return await connection.ExecuteScalarAsync<string?>("""
            SELECT p.code FROM valorapesquisa.subscriptions s
            JOIN valorapesquisa.plans p ON p.id = s.plan_id
            WHERE s.organization_id = @organizationId AND s.status = 'active' AND s.deleted_at IS NULL
            ORDER BY s.created_at DESC LIMIT 1
            """, new { organizationId });
    }

    public async Task CreateSubscriptionAsync(Guid organizationId, string planId)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("""
            INSERT INTO valorapesquisa.subscriptions(organization_id, plan_id, status)
            SELECT @organizationId, id, 'active' FROM valorapesquisa.plans WHERE code = @planId
            ON CONFLICT (organization_id) WHERE deleted_at IS NULL
            DO UPDATE SET plan_id = EXCLUDED.plan_id, status = 'active', updated_at = now()
            """, new { organizationId, planId });
    }
}
