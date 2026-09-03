using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.Enterprise;

namespace Valora.Infrastructure.Repositories;

public sealed class EnterpriseRepository(IDbConnectionFactory connections) : IEnterpriseRepository
{
    public async Task<PortfolioSummary> SummaryAsync(CancellationToken ct)
    {
        const string sql = """
        SELECT count(*)::int AS Companies,
          count(*) FILTER (WHERE account_status='at_risk')::int AS AtRisk,
          count(*) FILTER (WHERE account_status='trial' AND trial_ends_at < now()+interval '14 days')::int AS TrialsEnding,
          count(*) FILTER (WHERE COALESCE(usage_percent,0)>=80)::int AS NearLimit,
          (SELECT count(*)::int FROM valorapesquisa.crm_leads WHERE deleted_at IS NULL AND commercial_status NOT IN ('won','lost','active_customer')) AS ActiveLeads,
          COALESCE((SELECT sum(contracted_value) FROM valorapesquisa.subscriptions WHERE status='active' AND deleted_at IS NULL),0) AS MonthlyRecurringRevenue
        FROM valorapesquisa.organizations WHERE deleted_at IS NULL
        """;
        using var connection = connections.Create();
        return await connection.QuerySingleAsync<PortfolioSummary>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<EnterprisePage<PortfolioCompany>> CompaniesAsync(EnterpriseListQuery q, CancellationToken ct)
    {
        const string where = """
         WHERE o.deleted_at IS NULL
           AND (@Search::text IS NULL
                OR o.name ILIKE '%' || @Search::text || '%'
                OR COALESCE(o.cnpj, '') ILIKE '%' || @Search::text || '%'
                OR COALESCE(o.email, '') ILIKE '%' || @Search::text || '%')
           AND (@Status::text IS NULL OR o.account_status = @Status::text)
           AND (@PlanCode::text IS NULL OR p.code = @PlanCode::text)
           AND (@Health::text IS NULL OR o.account_health = @Health::text)
           AND (@From::timestamp IS NULL OR o.created_at >= @From::timestamp)
           AND (@ToExclusive::timestamp IS NULL OR o.created_at < @ToExclusive::timestamp)
        """;
        // Dapper does not support DateOnly as a parameter without a provider-specific handler.
        // Keep the public date-only contract, but send timestamp boundaries that also preserve
        // the index-friendly half-open interval used by PostgreSQL.
        var from = q.From?.ToDateTime(TimeOnly.MinValue);
        var toExclusive = q.To?.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var args = new { q.Search, q.Status, PlanCode = q.Plan, q.Health, From = from, ToExclusive = toExclusive, Offset=(q.Page-1)*q.PageSize, q.PageSize };
        const string select = """
        SELECT o.id AS "Id",
               o.name AS "Name",
               o.cnpj AS "Cnpj",
               o.email AS "Email",
               o.account_status AS "Status",
               o.account_health AS "Health",
               p.code AS "PlanCode",
               p.name AS "PlanName",
               o.created_at AS "CreatedAt",
               o.last_activity_at AS "LastActivityAt",
               COALESCE(o.usage_percent, 0)::int AS "UsagePercent"
          FROM valorapesquisa.organizations o
          LEFT JOIN valorapesquisa.subscriptions s ON s.organization_id = o.id AND s.deleted_at IS NULL
          LEFT JOIN valorapesquisa.plans p ON p.id = s.plan_id
        """;
        using var connection = connections.Create();
        var items = (await connection.QueryAsync<PortfolioCompany>(new CommandDefinition(select+where+" ORDER BY o.created_at DESC LIMIT @PageSize::int OFFSET @Offset::int", args, cancellationToken:ct))).ToList();
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(*)::int FROM valorapesquisa.organizations o LEFT JOIN valorapesquisa.subscriptions s ON s.organization_id=o.id AND s.deleted_at IS NULL LEFT JOIN valorapesquisa.plans p ON p.id=s.plan_id"+where,args,cancellationToken:ct));
        return new(items,total,q.Page,q.PageSize);
    }

    public async Task<EnterprisePage<CrmLead>> LeadsAsync(EnterpriseListQuery q, CancellationToken ct)
    {
        const string where=" WHERE deleted_at IS NULL AND (@Search::text IS NULL OR name ILIKE '%'||@Search::text||'%' OR COALESCE(company_name,'') ILIKE '%'||@Search::text||'%' OR COALESCE(email,'') ILIKE '%'||@Search::text||'%') AND (@Status::text IS NULL OR commercial_status=@Status::text)";
        var args=new{q.Search,q.Status,Offset=(q.Page-1)*q.PageSize,q.PageSize}; using var c=connections.Create();
        var rows=(await c.QueryAsync<CrmLead>(new CommandDefinition("SELECT id AS \"Id\",name AS \"Name\",company_name AS \"CompanyName\",email AS \"Email\",phone AS \"Phone\",commercial_status AS \"Status\",intended_plan AS \"IntendedPlan\",owner_name AS \"Owner\",next_action_at AS \"NextActionAt\",notes AS \"Notes\",created_at AS \"CreatedAt\" FROM valorapesquisa.crm_leads"+where+" ORDER BY created_at DESC LIMIT @PageSize::int OFFSET @Offset::int",args,cancellationToken:ct))).ToList();
        var total=await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(*)::int FROM valorapesquisa.crm_leads"+where,args,cancellationToken:ct)); return new(rows,total,q.Page,q.PageSize);
    }

    public async Task<Guid> CreateLeadAsync(CrmLead lead, CancellationToken ct)
    { const string sql="INSERT INTO valorapesquisa.crm_leads(id,name,company_name,email,phone,commercial_status,intended_plan,owner_name,next_action_at,notes) VALUES(@Id,@Name,@CompanyName,@Email,@Phone,@Status,@IntendedPlan,@Owner,@NextActionAt,@Notes) RETURNING id"; using var c=connections.Create(); return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,lead,cancellationToken:ct)); }

    public async Task UpdateCompanyStatusAsync(Guid id,string status,CancellationToken ct)
    { using var c=connections.Create(); var changed=await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.organizations SET account_status=@status,updated_at=now() WHERE id=@id AND deleted_at IS NULL",new{id,status},cancellationToken:ct)); if(changed==0) throw new KeyNotFoundException("Empresa não encontrada."); }

    public async Task<IReadOnlyList<EnterpriseItem>> ListItemsAsync(Guid? organizationId,string kind,CancellationToken ct)
    { using var c=connections.Create(); var rows=await c.QueryAsync<EnterpriseRow>(new CommandDefinition("SELECT id,organization_id OrganizationId,kind,name,status,configuration::text Configuration,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.enterprise_items WHERE kind=@kind AND deleted_at IS NULL AND (@organizationId::uuid IS NULL OR organization_id=@organizationId::uuid) ORDER BY name",new{organizationId,kind},cancellationToken:ct)); return rows.Select(Map).ToList(); }

    public async Task<Guid> UpsertItemAsync(Guid? organizationId,Guid? id,UpsertEnterpriseItemRequest request,CancellationToken ct)
    { const string sql="""INSERT INTO valorapesquisa.enterprise_items(id,organization_id,kind,name,status,configuration) VALUES(COALESCE(@id,gen_random_uuid()),@organizationId,@Kind,@Name,@Status,CAST(@Configuration AS jsonb)) ON CONFLICT(id) DO UPDATE SET name=EXCLUDED.name,status=EXCLUDED.status,configuration=EXCLUDED.configuration,updated_at=now() WHERE enterprise_items.organization_id IS NOT DISTINCT FROM @organizationId RETURNING id"""; using var c=connections.Create(); return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,new{id,organizationId,request.Kind,request.Name,request.Status,Configuration=request.Configuration.GetRawText()},cancellationToken:ct)); }

    public async Task SetItemStatusAsync(Guid? organizationId,Guid id,string status,CancellationToken ct)
    { using var c=connections.Create(); await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.enterprise_items SET status=@status,updated_at=now() WHERE id=@id AND organization_id IS NOT DISTINCT FROM @organizationId AND deleted_at IS NULL",new{organizationId,id,status},cancellationToken:ct)); }

    public async Task<Guid> CreateApiKeyAsync(Guid organizationId,string name,IReadOnlyList<string> scopes,string hash,string prefix,DateTime? expiresAt,Guid createdBy,CancellationToken ct)
    { const string sql="INSERT INTO valorapesquisa.api_keys(organization_id,name,key_prefix,key_hash,scopes,status,expires_at,created_by) VALUES(@organizationId,@name,@prefix,@hash,@scopes,'active',@expiresAt,@createdBy) RETURNING id"; using var c=connections.Create(); return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,new{organizationId,name,prefix,hash,scopes=scopes.ToArray(),expiresAt,createdBy},cancellationToken:ct)); }

    public async Task<IReadOnlyList<ApiKeySummary>> ListApiKeysAsync(Guid organizationId,CancellationToken ct)
    { const string sql="SELECT id Id,name Name,key_prefix Prefix,scopes Scopes,CASE WHEN revoked_at IS NOT NULL THEN 'revoked' WHEN expires_at IS NOT NULL AND expires_at<=now() THEN 'expired' ELSE 'active' END Status,created_at CreatedAt,expires_at ExpiresAt,last_used_at LastUsedAt FROM valorapesquisa.api_keys WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at DESC"; using var c=connections.Create(); return (await c.QueryAsync<ApiKeySummary>(new CommandDefinition(sql,new{organizationId},cancellationToken:ct))).ToList(); }

    public async Task<ApiKeySummary?> GetApiKeyAsync(Guid organizationId,Guid id,CancellationToken ct)
    { const string sql="SELECT id Id,name Name,key_prefix Prefix,scopes Scopes,CASE WHEN revoked_at IS NOT NULL THEN 'revoked' WHEN expires_at IS NOT NULL AND expires_at<=now() THEN 'expired' ELSE status END Status,created_at CreatedAt,expires_at ExpiresAt,last_used_at LastUsedAt FROM valorapesquisa.api_keys WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL"; using var c=connections.Create(); return await c.QuerySingleOrDefaultAsync<ApiKeySummary>(new CommandDefinition(sql,new{organizationId,id},cancellationToken:ct)); }

    public async Task<IReadOnlyList<ApiKeyUsage>> ListApiKeyUsageAsync(Guid organizationId,Guid id,CancellationToken ct)
    { const string sql="SELECT id Id,endpoint Endpoint,method Method,status_code StatusCode,scope_used ScopeUsed,correlation_id CorrelationId,created_at CreatedAt FROM valorapesquisa.api_key_usage_events WHERE organization_id=@organizationId AND api_key_id=@id AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 100"; using var c=connections.Create(); return (await c.QueryAsync<ApiKeyUsage>(new CommandDefinition(sql,new{organizationId,id},cancellationToken:ct))).ToList(); }

    public async Task<bool> RevokeApiKeyAsync(Guid organizationId,Guid id,CancellationToken ct)
    { const string sql="UPDATE valorapesquisa.api_keys SET revoked_at=now(),status='revoked',updated_at=now() WHERE id=@id AND organization_id=@organizationId AND revoked_at IS NULL AND deleted_at IS NULL"; using var c=connections.Create(); return await c.ExecuteAsync(new CommandDefinition(sql,new{organizationId,id},cancellationToken:ct))==1; }

    private static EnterpriseItem Map(EnterpriseRow x)=>new(x.Id,x.OrganizationId,x.Kind,x.Name,x.Status,JsonDocument.Parse(x.Configuration).RootElement.Clone(),x.CreatedAt,x.UpdatedAt);
    private sealed record EnterpriseRow(Guid Id,Guid? OrganizationId,string Kind,string Name,string Status,string Configuration,DateTime CreatedAt,DateTime UpdatedAt);
}
