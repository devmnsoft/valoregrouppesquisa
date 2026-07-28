using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Infrastructure.Repositories;
public sealed class DashboardMetricsRepository(IDbConnectionFactory connections):IDashboardMetricsRepository
{
 public async Task<DashboardMetricsDto> GetGlobalAsync(){using var c=connections.Create();return await c.QueryFirstAsync<DashboardMetricsDto>("SELECT (SELECT count(*) FROM valorapesquisa.organizations)::int AS Organizations,(SELECT count(*) FROM valorapesquisa.users)::int AS Users,(SELECT count(*) FROM valorapesquisa.surveys)::int AS Surveys,(SELECT count(*) FROM valorapesquisa.responses)::int AS Responses,(SELECT count(*) FROM valorapesquisa.subscriptions WHERE status='active' AND deleted_at IS NULL)::int AS ActiveSubscriptions");}
 public async Task<DashboardMetricsDto> GetOrganizationAsync(Guid organizationId){using var c=connections.Create();return await c.QueryFirstAsync<DashboardMetricsDto>("SELECT 1 AS Organizations,(SELECT count(*) FROM valorapesquisa.users WHERE organization_id=@organizationId)::int AS Users,(SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@organizationId)::int AS Surveys,(SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@organizationId)::int AS Responses,(SELECT count(*) FROM valorapesquisa.subscriptions WHERE organization_id=@organizationId AND status='active' AND deleted_at IS NULL)::int AS ActiveSubscriptions",new { organizationId });}
}
