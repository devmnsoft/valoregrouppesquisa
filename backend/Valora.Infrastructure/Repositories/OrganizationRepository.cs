using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Application.Security;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationRepository(IDbConnectionFactory factory, ILogger<OrganizationRepository> logger) : IOrganizationRepository
{
    public async Task<OrganizationRecord?> GetAsync(Guid id)
    {
        using var c=factory.Create();
        const string sql="SELECT id AS Id,name AS Name,public_name AS PublicName,slug AS Slug,email AS Email,phone AS Phone,status AS Status,default_language_code AS DefaultLanguageCode,time_zone AS TimeZone,onboarding_status AS OnboardingStatus,created_at AS CreatedAt,updated_at AS UpdatedAt FROM valorapesquisa.organizations WHERE id=@id AND deleted_at IS NULL";
        return await c.QuerySingleOrDefaultAsync<OrganizationRecord>(sql,new{id});
    }

    public async Task<Guid> CreateAsync(string name,string email,string slug,string planId)
    {
        using var c=factory.Create();
        return await c.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.organizations(name,public_name,email,slug) VALUES (@name,@name,@email,@slug) RETURNING id",new{name,email,slug});
    }

    public async Task UpdateCurrentAsync(Guid id,UpdateOrganizationRequest request)
    {
        using var c=factory.Create();
        await c.ExecuteAsync("UPDATE valorapesquisa.organizations SET public_name=COALESCE(@PublicName,public_name),phone=COALESCE(@Phone,phone),updated_at=now() WHERE id=@id AND deleted_at IS NULL",new{request.PublicName,request.Phone,id});
    }

    public async Task<int> CountManagersAsync(Guid organizationId)
    {
        using var c=factory.Create();
        return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE u.organization_id=@organizationId AND u.deleted_at IS NULL AND u.status='active' AND r.code='empresa_admin'",new{organizationId});
    }

    public async Task<IReadOnlyList<OrganizationSettingRecord>> GetSettingsAsync(Guid organizationId)
    {
        using var c=factory.Create();
        var rows=await c.QueryAsync<OrganizationSettingRecord>("SELECT id AS Id,settings::text AS Settings,updated_at AS UpdatedAt FROM valorapesquisa.organization_settings WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY updated_at DESC LIMIT 1",new{organizationId});
        return rows.AsList();
    }

    public async Task UpsertSettingsAsync(Guid organizationId,IReadOnlyDictionary<string,object?> settings)
    {
        using var c=factory.Create();
        var safe=settings.Where(x=>!x.Key.Contains("password",StringComparison.OrdinalIgnoreCase)&&!x.Key.Contains("token",StringComparison.OrdinalIgnoreCase)&&!x.Key.Contains("secret",StringComparison.OrdinalIgnoreCase)).ToDictionary(x=>x.Key,x=>x.Value);
        await c.ExecuteAsync("INSERT INTO valorapesquisa.organization_settings(organization_id,settings) VALUES (@organizationId,CAST(@settingsJson AS jsonb)) ON CONFLICT (organization_id) DO UPDATE SET settings=EXCLUDED.settings,updated_at=now()",new{organizationId,settingsJson=System.Text.Json.JsonSerializer.Serialize(safe)});
    }

    public async Task<IReadOnlyList<OrganizationUsageRecord>> GetUsageAsync(Guid organizationId)
    {
        using var c=factory.Create();
        var rows=await c.QueryAsync<OrganizationUsageRecord>("SELECT metric_key AS MetricKey,metric_value AS MetricValue,period_month AS PeriodMonth,updated_at AS UpdatedAt FROM valorapesquisa.usage_monthly WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY period_month DESC,metric_key",new{organizationId});
        return rows.AsList();
    }
}
