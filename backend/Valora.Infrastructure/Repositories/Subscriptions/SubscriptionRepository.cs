using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;
public sealed class SubscriptionRepository(IDbConnectionFactory connections) : ISubscriptionRepository
{
    public async Task<SubscriptionDto?> GetByOrganizationAsync(Guid organizationId)
    {
        using var connection = connections.Create();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionDto>("SELECT id AS Id,organization_id AS OrganizationId,plan_id AS PlanId,status AS Status,starts_at AS StartsAt,ends_at AS EndsAt FROM valorapesquisa.subscriptions WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1",new { organizationId });
    }
    public async Task UpsertAsync(SubscriptionDto subscription)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("""INSERT INTO valorapesquisa.subscriptions(id,organization_id,plan_id,status,starts_at,ends_at) VALUES(@Id,@OrganizationId,@PlanId,@Status,@StartsAt,@EndsAt) ON CONFLICT (organization_id) WHERE deleted_at IS NULL DO UPDATE SET plan_id=EXCLUDED.plan_id,status=EXCLUDED.status,starts_at=EXCLUDED.starts_at,ends_at=EXCLUDED.ends_at,updated_at=now()""",subscription);
    }
    public async Task SetStatusAsync(Guid organizationId,string status)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("UPDATE valorapesquisa.subscriptions SET status=@status,updated_at=now() WHERE organization_id=@organizationId AND deleted_at IS NULL",new { organizationId,status });
    }
}
