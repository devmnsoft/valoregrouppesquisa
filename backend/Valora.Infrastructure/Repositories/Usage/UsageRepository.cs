using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;
internal sealed record UsageRow(string UsageKey,long Quantity);
public sealed class UsageRepository(IDbConnectionFactory connections) : IUsageRepository
{
    public async Task<UsageDto> GetMonthlyAsync(Guid organizationId,DateTime month)
    {
        using var connection=connections.Create();
        var values=(await connection.QueryAsync<UsageRow>("SELECT usage_key AS UsageKey,quantity AS Quantity FROM valorapesquisa.usage_monthly WHERE organization_id=@organizationId AND year=@year AND month=@month",new { organizationId,year=month.Year,month=month.Month })).ToDictionary(x=>x.UsageKey,x=>checked((int)x.Quantity));
        int Get(string key)=>values.GetValueOrDefault(key);
        return new UsageDto(Get("active_surveys"),Get("responses"),Get("managers"),values);
    }
    public async Task RecalculateAsync(Guid organizationId,DateTime month)
    {
        using var connection=connections.Create();
        await connection.ExecuteAsync("""INSERT INTO valorapesquisa.usage_monthly(organization_id,usage_key,year,month,quantity) VALUES (@organizationId,'active_surveys',@year,@month,(SELECT count(*) FROM valorapesquisa.surveys WHERE organization_id=@organizationId AND status='active')),(@organizationId,'responses',@year,@month,(SELECT count(*) FROM valorapesquisa.responses WHERE organization_id=@organizationId AND EXTRACT(YEAR FROM created_at)=@year AND EXTRACT(MONTH FROM created_at)=@month)),(@organizationId,'managers',@year,@month,(SELECT count(DISTINCT ur.user_id) FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE ur.organization_id=@organizationId AND r.code='empresa_admin')) ON CONFLICT (organization_id,usage_key,year,month) DO UPDATE SET quantity=EXCLUDED.quantity,updated_at=now()""",new { organizationId,year=month.Year,month=month.Month });
    }
}
