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
        const string where = """ WHERE o.deleted_at IS NULL AND (@Search IS NULL OR o.name ILIKE '%'||@Search||'%' OR COALESCE(o.cnpj,'') ILIKE '%'||@Search||'%' OR COALESCE(o.email,'') ILIKE '%'||@Search||'%') AND (@Status IS NULL OR o.account_status=@Status) AND (@Plan IS NULL OR p.name=@Plan) AND (@Health IS NULL OR o.account_health=@Health) AND (@From IS NULL OR o.created_at>=@From) AND (@To IS NULL OR o.created_at<@To::date+1) """;
        var args = new { q.Search, q.Status, q.Plan, q.Health, q.From, q.To, Offset=(q.Page-1)*q.PageSize, q.PageSize };
        const string select = """SELECT o.id Id,o.name Name,o.cnpj Cnpj,o.email Email,o.account_status Status,o.account_health Health,p.name Plan,o.created_at CreatedAt,o.last_activity_at LastActivityAt,COALESCE(o.usage_percent,0) UsagePercent FROM valorapesquisa.organizations o LEFT JOIN valorapesquisa.subscriptions s ON s.organization_id=o.id AND s.deleted_at IS NULL LEFT JOIN valorapesquisa.plans p ON p.id=s.plan_id""";
        using var connection = connections.Create();
        var items = (await connection.QueryAsync<PortfolioCompany>(new CommandDefinition(select+where+" ORDER BY o.created_at DESC OFFSET @Offset LIMIT @PageSize", args, cancellationToken:ct))).ToList();
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(*) FROM valorapesquisa.organizations o LEFT JOIN valorapesquisa.subscriptions s ON s.organization_id=o.id AND s.deleted_at IS NULL LEFT JOIN valorapesquisa.plans p ON p.id=s.plan_id"+where,args,cancellationToken:ct));
        return new(items,total,q.Page,q.PageSize);
    }

    public async Task<EnterprisePage<CrmLead>> LeadsAsync(EnterpriseListQuery q, CancellationToken ct)
    {
        const string where=" WHERE deleted_at IS NULL AND (@Search IS NULL OR name ILIKE '%'||@Search||'%' OR COALESCE(company_name,'') ILIKE '%'||@Search||'%' OR COALESCE(email,'') ILIKE '%'||@Search||'%') AND (@Status IS NULL OR commercial_status=@Status)";
        var args=new{q.Search,q.Status,Offset=(q.Page-1)*q.PageSize,q.PageSize}; using var c=connections.Create();
        var rows=(await c.QueryAsync<CrmLead>(new CommandDefinition("SELECT id Id,name Name,company_name CompanyName,email Email,phone Phone,commercial_status Status,intended_plan IntendedPlan,owner_name Owner,next_action_at NextActionAt,notes Notes,created_at CreatedAt FROM valorapesquisa.crm_leads"+where+" ORDER BY created_at DESC OFFSET @Offset LIMIT @PageSize",args,cancellationToken:ct))).ToList();
        var total=await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(*) FROM valorapesquisa.crm_leads"+where,args,cancellationToken:ct)); return new(rows,total,q.Page,q.PageSize);
    }

    public async Task<Guid> CreateLeadAsync(CrmLead lead, CancellationToken ct)
    { const string sql="INSERT INTO valorapesquisa.crm_leads(id,name,company_name,email,phone,commercial_status,intended_plan,owner_name,next_action_at,notes) VALUES(@Id,@Name,@CompanyName,@Email,@Phone,@Status,@IntendedPlan,@Owner,@NextActionAt,@Notes) RETURNING id"; using var c=connections.Create(); return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,lead,cancellationToken:ct)); }

    public async Task UpdateCompanyStatusAsync(Guid id,string status,CancellationToken ct)
    { using var c=connections.Create(); var changed=await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.organizations SET account_status=@status,updated_at=now() WHERE id=@id AND deleted_at IS NULL",new{id,status},cancellationToken:ct)); if(changed==0) throw new KeyNotFoundException("Empresa não encontrada."); }

    public async Task<IReadOnlyList<EnterpriseItem>> ListItemsAsync(Guid? organizationId,string kind,CancellationToken ct)
    { using var c=connections.Create(); var rows=await c.QueryAsync<EnterpriseRow>(new CommandDefinition("SELECT id,organization_id OrganizationId,kind,name,status,configuration::text Configuration,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.enterprise_items WHERE kind=@kind AND deleted_at IS NULL AND (@organizationId IS NULL OR organization_id=@organizationId) ORDER BY name",new{organizationId,kind},cancellationToken:ct)); return rows.Select(Map).ToList(); }

    public async Task<Guid> UpsertItemAsync(Guid? organizationId,Guid? id,UpsertEnterpriseItemRequest request,CancellationToken ct)
    { const string sql="""INSERT INTO valorapesquisa.enterprise_items(id,organization_id,kind,name,status,configuration) VALUES(COALESCE(@id,gen_random_uuid()),@organizationId,@Kind,@Name,@Status,CAST(@Configuration AS jsonb)) ON CONFLICT(id) DO UPDATE SET name=EXCLUDED.name,status=EXCLUDED.status,configuration=EXCLUDED.configuration,updated_at=now() WHERE enterprise_items.organization_id IS NOT DISTINCT FROM @organizationId RETURNING id"""; using var c=connections.Create(); return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,new{id,organizationId,request.Kind,request.Name,request.Status,Configuration=request.Configuration.GetRawText()},cancellationToken:ct)); }

    public async Task SetItemStatusAsync(Guid? organizationId,Guid id,string status,CancellationToken ct)
    { using var c=connections.Create(); await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.enterprise_items SET status=@status,updated_at=now() WHERE id=@id AND organization_id IS NOT DISTINCT FROM @organizationId AND deleted_at IS NULL",new{organizationId,id,status},cancellationToken:ct)); }

    public async Task<ApiKeyIssued> CreateApiKeyAsync(Guid organizationId,string name,IReadOnlyList<string> scopes,string hash,string prefix,string secret,CancellationToken ct)
    { const string sql="INSERT INTO valorapesquisa.api_keys(organization_id,name,key_prefix,key_hash,scopes) VALUES(@organizationId,@name,@prefix,@hash,@scopes) RETURNING id"; using var c=connections.Create(); var id=await c.ExecuteScalarAsync<Guid>(new CommandDefinition(sql,new{organizationId,name,prefix,hash,scopes=scopes.ToArray()},cancellationToken:ct)); return new(id,name,prefix,secret,scopes,DateTime.UtcNow); }

    private static EnterpriseItem Map(EnterpriseRow x)=>new(x.Id,x.OrganizationId,x.Kind,x.Name,x.Status,JsonDocument.Parse(x.Configuration).RootElement.Clone(),x.CreatedAt,x.UpdatedAt);
    private sealed record EnterpriseRow(Guid Id,Guid? OrganizationId,string Kind,string Name,string Status,string Configuration,DateTime CreatedAt,DateTime UpdatedAt);
}
