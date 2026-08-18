using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;

public sealed class AssistedOperationsRepository(IDbConnectionFactory connections) : IAssistedOperationsRepository
{
    private static readonly IReadOnlyDictionary<string, (string Table, string[] Writable)> Resources =
        new Dictionary<string, (string, string[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["tickets"] = ("support_tickets", ["subject","description","type","priority","status","module","route","assigned_user_id","resolution_summary","reopen_reason","metadata_json"]),
            ["comments"] = ("support_ticket_comments", ["ticket_id","comment","metadata_json"]),
            ["feedback"] = ("customer_feedback", ["type","rating","module","message","blocked_usage","status","metadata_json","converted_ticket_id"]),
            ["onboarding"] = ("onboarding_checklists", ["status","notes","blocked_reason"]),
            ["upgrade-requests"] = ("upgrade_requests", ["type","current_plan","requested_resource","status","assigned_user_id","notes","usage_event_id","metadata_json"]),
            ["incidents"] = ("operational_incidents", ["title","description","severity","status","assigned_user_id","root_cause","corrective_action","lessons_learned","resolution_summary","metadata_json"]),
            ["release-notes"] = ("release_notes", ["version","title","content","type","visibility","status","release_date","published_at","metadata_json"]),
            ["data-quality"] = ("data_quality_issues", ["run_id","check_code","entity_type","entity_id","severity","description","status","metadata_json"])
        };

    public async Task<IReadOnlyList<IDictionary<string, object?>>> ListAsync(string resource, Guid? organizationId, CancellationToken ct = default)
    {
        var definition = Definition(resource);
        using var db = connections.Create();
        var orgClause = organizationId.HasValue && resource is not "release-notes" ? " WHERE organization_id=@OrganizationId" : "";
        var rows = await db.QueryAsync(new CommandDefinition($"SELECT * FROM valorapesquisa.{definition.Table}{orgClause} ORDER BY created_at DESC LIMIT 200", new { OrganizationId = organizationId }, cancellationToken: ct));
        return rows.Select(ToDictionary).ToArray();
    }

    public async Task<IDictionary<string, object?>?> GetAsync(string resource, Guid id, Guid? organizationId, CancellationToken ct = default)
    {
        var definition = Definition(resource);
        using var db = connections.Create();
        var orgClause = organizationId.HasValue && resource is not "release-notes" ? " AND organization_id=@OrganizationId" : "";
        var row = await db.QuerySingleOrDefaultAsync(new CommandDefinition($"SELECT * FROM valorapesquisa.{definition.Table} WHERE id=@Id{orgClause}", new { Id = id, OrganizationId = organizationId }, cancellationToken: ct));
        return row is null ? null : ToDictionary(row);
    }

    public async Task<Guid> CreateAsync(string resource, Guid? organizationId, Guid? userId, IReadOnlyDictionary<string, object?> values, string correlationId, CancellationToken ct = default)
    {
        var definition = Definition(resource);
        var accepted = values.Where(x => definition.Writable.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToArray();
        var columns = new List<string>(); var parameters = new DynamicParameters();
        if (resource is not "release-notes") { columns.Add("organization_id"); parameters.Add("OrganizationId", organizationId); }
        if (resource is "tickets" or "comments" or "feedback" or "upgrade-requests") { columns.Add("user_id"); parameters.Add("UserId", userId); }
        foreach (var item in accepted) { columns.Add(item.Key); parameters.Add(item.Key, Normalize(item)); }
        columns.Add("correlation_id"); parameters.Add("CorrelationId", correlationId);
        var sql = $"INSERT INTO valorapesquisa.{definition.Table} ({string.Join(',', columns)}) VALUES ({string.Join(',', columns.Select(x => '@' + x))}) RETURNING id";
        using var db = connections.Create();
        var id = await db.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        await Audit(db, organizationId, userId, $"{resource}.created", resource, id, correlationId, ct);
        if (resource == "tickets" && values.TryGetValue("priority", out var priority) && string.Equals(priority?.ToString(), "critical", StringComparison.OrdinalIgnoreCase))
            await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.notifications(organization_id,user_id,title,message,status,correlation_id) VALUES(@OrganizationId,@UserId,'Chamado crítico aberto','Um chamado crítico requer triagem imediata.','unread',@CorrelationId)",new {OrganizationId=organizationId,UserId=userId,CorrelationId=correlationId},cancellationToken:ct));
        return id;
    }

    public async Task<bool> UpdateAsync(string resource, Guid id, Guid? organizationId, IReadOnlyDictionary<string, object?> values, string action, string correlationId, CancellationToken ct = default)
    {
        var definition = Definition(resource); var accepted = values.Where(x => definition.Writable.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (accepted.Length == 0) return false;
        var p = new DynamicParameters(new { Id = id, OrganizationId = organizationId });
        foreach (var item in accepted) p.Add(item.Key, Normalize(item));
        var orgClause = organizationId.HasValue && resource is not "release-notes" ? " AND organization_id=@OrganizationId" : "";
        using var db = connections.Create();
        var changed = await db.ExecuteAsync(new CommandDefinition($"UPDATE valorapesquisa.{definition.Table} SET {string.Join(',', accepted.Select(x => x.Key + "=@" + x.Key))}, updated_at=now() WHERE id=@Id{orgClause}", p, cancellationToken: ct)) > 0;
        if (changed) await Audit(db, organizationId, null, $"{resource}.{action}", resource, id, correlationId, ct);
        return changed;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> CustomerHealthAsync(Guid? organizationId, CancellationToken ct = default)
    {
        using var db = connections.Create();
        const string sql = """SELECT o.id organization_id,o.name organization_name,p.name plan_name,count(DISTINCT u.id) active_users,count(DISTINCT s.id) diagnostics,count(DISTINCT r.id) responses,count(DISTINCT t.id) FILTER (WHERE t.status NOT IN ('closed','resolved','cancelled')) open_tickets,count(DISTINCT t.id) FILTER (WHERE t.priority='critical' AND t.status NOT IN ('closed','resolved','cancelled')) critical_tickets,CASE WHEN count(DISTINCT s.id)=0 THEN 'onboarding' WHEN count(DISTINCT t.id) FILTER (WHERE t.priority='critical' AND t.status NOT IN ('closed','resolved','cancelled'))>0 THEN 'critical' WHEN count(DISTINCT r.id)=0 THEN 'risk' ELSE 'healthy' END health_status FROM valorapesquisa.organizations o LEFT JOIN valorapesquisa.subscriptions sub ON sub.organization_id=o.id LEFT JOIN valorapesquisa.plans p ON p.id=sub.plan_id LEFT JOIN valorapesquisa.users u ON u.organization_id=o.id AND u.status='active' LEFT JOIN valorapesquisa.surveys s ON s.organization_id=o.id LEFT JOIN valorapesquisa.responses r ON r.survey_id=s.id LEFT JOIN valorapesquisa.support_tickets t ON t.organization_id=o.id WHERE (@OrganizationId IS NULL OR o.id=@OrganizationId) GROUP BY o.id,o.name,p.name ORDER BY critical_tickets DESC, organization_name""";
        var rows = await db.QueryAsync(new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: ct)); return rows.Select(ToDictionary).ToArray();
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> UsageAsync(Guid? organizationId, CancellationToken ct = default)
    { using var db = connections.Create(); var rows=await db.QueryAsync(new CommandDefinition("SELECT organization_id,period_start,period_end,active_users,logins,diagnostics_created,diagnostics_published,public_links,responses,reports_generated,certificates_generated,actions_created,actions_completed,blocked_features FROM valorapesquisa.usage_analytics_snapshots WHERE (@OrganizationId IS NULL OR organization_id=@OrganizationId) ORDER BY period_start DESC LIMIT 100",new {OrganizationId=organizationId},cancellationToken:ct)); return rows.Select(ToDictionary).ToArray(); }

    public async Task<Guid> RunDataQualityAsync(Guid? userId, string correlationId, CancellationToken ct = default)
    { using var db=connections.Create(); var id=await db.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.data_quality_runs(status,started_at,created_at,correlation_id,created_by_user_id) VALUES ('completed',now(),now(),@CorrelationId,@UserId) RETURNING id",new{CorrelationId=correlationId,UserId=userId},cancellationToken:ct)); await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.data_quality_issues(run_id,check_code,entity_type,entity_id,severity,description,status,created_at) SELECT @Id,'organization_without_plan','organization',o.id,'high','Organização sem plano ativo','open',now() FROM valorapesquisa.organizations o WHERE NOT EXISTS(SELECT 1 FROM valorapesquisa.subscriptions s WHERE s.organization_id=o.id AND s.status='active')",new{Id=id},cancellationToken:ct)); await Audit(db,null,userId,"data-quality.executed","data_quality_run",id,correlationId,ct); return id; }

    private static (string Table,string[] Writable) Definition(string resource) => Resources.TryGetValue(resource,out var value)?value:throw new ArgumentOutOfRangeException(nameof(resource));
    private static object? Normalize(KeyValuePair<string,object?> item) => item.Key.EndsWith("_json",StringComparison.OrdinalIgnoreCase) && item.Value is not null ? JsonSerializer.Serialize(item.Value) : item.Value;
    private static Dictionary<string,object?> ToDictionary(dynamic row) => ((IDictionary<string,object?>)row).ToDictionary(x=>x.Key,x=>x.Value,StringComparer.OrdinalIgnoreCase);
    private static async Task Audit(System.Data.IDbConnection db,Guid? organizationId,Guid? userId,string action,string entityType,Guid entityId,string correlationId,CancellationToken ct) => await db.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,correlation_id,metadata_json) VALUES(@OrganizationId,@UserId,@Action,@EntityType,@EntityId,@CorrelationId,'{}'::jsonb); INSERT INTO valorapesquisa.platform_governance_events(organization_id,user_id,action,entity_type,entity_id,correlation_id,metadata_json) VALUES(@OrganizationId,@UserId,@Action,@EntityType,@EntityId,@CorrelationId,'{}'::jsonb)",new{OrganizationId=organizationId,UserId=userId,Action=action,EntityType=entityType,EntityId=entityId,CorrelationId=correlationId},cancellationToken:ct));
}
