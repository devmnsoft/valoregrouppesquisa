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
        var affected = await c.ExecuteAsync("""
            UPDATE valorapesquisa.organizations SET
                public_name=COALESCE(@PublicName,public_name), phone=COALESCE(@Phone,phone),
                email=COALESCE(@Email,email), default_language_code=COALESCE(@DefaultLanguageCode,default_language_code),
                time_zone=COALESCE(@TimeZone,time_zone), version=version+1, updated_at=now()
            WHERE id=@id AND deleted_at IS NULL AND (@ExpectedVersion IS NULL OR version=@ExpectedVersion)
            """,new{request.PublicName,request.Phone,request.Email,request.DefaultLanguageCode,request.TimeZone,request.ExpectedVersion,id});
        if (affected == 0)
        {
            var exists = await c.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM valorapesquisa.organizations WHERE id=@id AND deleted_at IS NULL)", new { id });
            if (!exists) throw new KeyNotFoundException("Organização não encontrada.");
            throw new InvalidOperationException("A organização foi atualizada por outra sessão.");
        }
    }

    public async Task<int> CountManagersAsync(Guid organizationId)
    {
        using var c=factory.Create();
        return await c.ExecuteScalarAsync<int>("SELECT count(*) FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE u.organization_id=@organizationId AND u.deleted_at IS NULL AND u.status='active' AND r.code='empresa_admin'",new{organizationId});
    }

    public async Task<IReadOnlyList<OrganizationSettingRecord>> GetSettingsAsync(Guid organizationId)
    {
        using var c=factory.Create();
        var rows=await c.QueryAsync<OrganizationSettingRecord>("SELECT id AS Id,settings::text AS Settings,created_at AS CreatedAt,updated_at AS UpdatedAt FROM valorapesquisa.organization_settings WHERE organization_id=@organizationId",new{organizationId});
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
        const string sql = """
            WITH current_subscription AS (
                SELECT s.plan_id FROM valorapesquisa.subscriptions s
                WHERE s.organization_id=@organizationId AND s.deleted_at IS NULL
                ORDER BY s.starts_at DESC LIMIT 1
            ), usage_keys AS (
                SELECT usage_key AS key FROM valorapesquisa.usage_monthly WHERE organization_id=@organizationId
                UNION SELECT usage_key FROM valorapesquisa.usage_lifetime WHERE organization_id=@organizationId
                UNION SELECT metric_key FROM valorapesquisa.plan_usage_counters WHERE organization_id=@organizationId
                UNION SELECT pl.limit_key FROM valorapesquisa.plan_limits pl JOIN current_subscription cs ON cs.plan_id=pl.plan_id
            ), totals AS (
                SELECT k.key,
                    COALESCE((SELECT sum(quantity) FROM valorapesquisa.usage_monthly m WHERE m.organization_id=@organizationId AND m.usage_key=k.key AND m.year=EXTRACT(YEAR FROM CURRENT_DATE) AND m.month=EXTRACT(MONTH FROM CURRENT_DATE)),0)
                    + COALESCE((SELECT sum(quantity) FROM valorapesquisa.usage_lifetime l WHERE l.organization_id=@organizationId AND l.usage_key=k.key),0)
                    + COALESCE((SELECT sum(consumed) FROM valorapesquisa.plan_usage_counters c WHERE c.organization_id=@organizationId AND c.metric_key=k.key),0) AS consumed,
                    COALESCE((SELECT sum(reserved) FROM valorapesquisa.plan_usage_counters c WHERE c.organization_id=@organizationId AND c.metric_key=k.key),0)
                    + COALESCE((SELECT sum(quantity) FROM valorapesquisa.plan_usage_reservations r WHERE r.organization_id=@organizationId AND r.metric_key=k.key AND r.status='reserved' AND r.expires_at>now()),0) AS reserved,
                    (SELECT pl.limit_value FROM valorapesquisa.plan_limits pl JOIN current_subscription cs ON cs.plan_id=pl.plan_id WHERE pl.limit_key=k.key) AS limit_value
                FROM usage_keys k
            )
            SELECT key AS Key, to_char(CURRENT_DATE,'YYYY-MM') AS Period, consumed::bigint AS Consumed, reserved::bigint AS Reserved,
                limit_value AS Limit, CASE WHEN limit_value IS NULL THEN NULL ELSE GREATEST(limit_value-consumed-reserved,0)::bigint END AS Available,
                CASE WHEN limit_value IS NULL OR limit_value=0 THEN NULL ELSE round((consumed+reserved)*100.0/limit_value,2) END AS Percentage,
                (limit_value IS NULL) AS Unlimited
            FROM totals ORDER BY key
            """;
        var rows=await c.QueryAsync<OrganizationUsageRecord>(sql,new{organizationId});
        return rows.AsList();
    }
}
