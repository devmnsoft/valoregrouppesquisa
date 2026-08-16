using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
using Valora.Application.Security;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationRepository(IDbConnectionFactory factory, ILogger<OrganizationRepository> logger) : IOrganizationRepository
{
    public async Task<OrganizationRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                o.id AS "Id",
                o.name AS "Name",
                o.public_name AS "PublicName",
                o.slug AS "Slug",
                o.email AS "Email",
                o.phone AS "Phone",
                o.status AS "Status",
                o.default_language_code AS "DefaultLanguageCode",
                o.time_zone AS "TimeZone",
                o.onboarding_status AS "OnboardingStatus",
                o.created_at AS "CreatedAt",
                o.updated_at AS "UpdatedAt",
                o.version AS "Version"
                ,o.legal_name AS "LegalName", o.cnpj AS "Cnpj", o.segment AS "Segment", o.cnae AS "Cnae"
                ,o.company_size AS "CompanySize", o.approximate_employee_count AS "ApproximateEmployeeCount"
                ,o.leadership_count AS "LeadershipCount", o.business_model AS "BusinessModel", o.region AS "Region"
                ,o.city AS "City", o.state AS "State", o.primary_contact_name AS "PrimaryContactName"
                ,o.minimum_aggregation_size AS "MinimumAggregationSize"
            FROM valorapesquisa.organizations o
            WHERE o.id = @id
              AND o.deleted_at IS NULL
            LIMIT 1;
            """;

        try
        {
            using var connection = factory.Create();
            var command = new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken);
            var organization = await connection.QuerySingleOrDefaultAsync<OrganizationRecord>(command);
            logger.LogDebug("Organization lookup completed. OrganizationId={OrganizationId} Found={Found}", id, organization is not null);
            return organization;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Organization lookup failed. OrganizationId={OrganizationId}", id);
            throw;
        }
    }

    public async Task<Guid> CreateAsync(string name,string email,string slug,string planId)
    {
        using var c=factory.Create();
        return await c.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.organizations(name,public_name,email,slug) VALUES (@name,@name,@email,@slug) RETURNING id",new{name,email,slug});
    }

    public async Task<long?> UpdateCurrentAsync(Guid id,UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        using var c=factory.Create();
        return await c.QuerySingleOrDefaultAsync<long?>(new CommandDefinition("""
            UPDATE valorapesquisa.organizations SET
                public_name=COALESCE(@PublicName,public_name), phone=COALESCE(@Phone,phone),
                email=COALESCE(@Email,email), default_language_code=COALESCE(@DefaultLanguageCode,default_language_code),
                time_zone=COALESCE(@TimeZone,time_zone), version=version+1, updated_at=now()
                ,legal_name=COALESCE(@LegalName,legal_name), cnpj=COALESCE(@Cnpj,cnpj), segment=COALESCE(@Segment,segment)
                ,cnae=COALESCE(@Cnae,cnae), company_size=COALESCE(@CompanySize,company_size)
                ,approximate_employee_count=COALESCE(@ApproximateEmployeeCount,approximate_employee_count)
                ,leadership_count=COALESCE(@LeadershipCount,leadership_count), business_model=COALESCE(@BusinessModel,business_model)
                ,region=COALESCE(@Region,region), city=COALESCE(@City,city), state=COALESCE(@State,state)
                ,primary_contact_name=COALESCE(@PrimaryContactName,primary_contact_name)
                ,minimum_aggregation_size=COALESCE(@MinimumAggregationSize,minimum_aggregation_size)
            WHERE id=@id AND version=@ExpectedVersion AND deleted_at IS NULL
            RETURNING version
            """,new{request.PublicName,request.Phone,request.Email,request.DefaultLanguageCode,request.TimeZone,request.ExpectedVersion,
                request.LegalName,request.Cnpj,request.Segment,request.Cnae,request.CompanySize,request.ApproximateEmployeeCount,
                request.LeadershipCount,request.BusinessModel,request.Region,request.City,request.State,request.PrimaryContactName,
                request.MinimumAggregationSize,id}, cancellationToken: cancellationToken));
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
