using Dapper;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class SaasAdministrationRepository(IDbConnectionFactory factory) : ISaasAdministrationRepository
{
    public async Task<IReadOnlyList<SaasGovernanceEvent>> ListGovernanceAsync(Guid organizationId, bool global, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        using var connection = factory.Create();
        const string sql = """SELECT id Id,organization_id OrganizationId,user_id UserId,COALESCE(module,'platform') Module,COALESCE(entity_type,'platform') EntityType,entity_id EntityId,COALESCE(action,'platform.event') Action,before_json::text BeforeJson,after_json::text AfterJson,reason Reason,correlation_id CorrelationId,COALESCE(severity,'information') Severity,created_at CreatedAt FROM valorapesquisa.platform_governance_events WHERE deleted_at IS NULL AND (@global OR organization_id=@organizationId) AND (@action IS NULL OR action=@action) AND (@from IS NULL OR created_at>=@from) AND (@to IS NULL OR created_at<=@to) ORDER BY created_at DESC LIMIT 250""";
        return (await connection.QueryAsync<SaasGovernanceEvent>(new CommandDefinition(sql, new { organizationId, global, action, from, to }, cancellationToken: ct))).AsList();
    }

    public async Task<SaasGovernanceEvent?> GetGovernanceAsync(Guid organizationId, bool global, Guid id, CancellationToken ct)
    {
        using var connection = factory.Create();
        const string sql = """SELECT id Id,organization_id OrganizationId,user_id UserId,COALESCE(module,'platform') Module,COALESCE(entity_type,'platform') EntityType,entity_id EntityId,COALESCE(action,'platform.event') Action,before_json::text BeforeJson,after_json::text AfterJson,reason Reason,correlation_id CorrelationId,COALESCE(severity,'information') Severity,created_at CreatedAt FROM valorapesquisa.platform_governance_events WHERE id=@id AND deleted_at IS NULL AND (@global OR organization_id=@organizationId)""";
        return await connection.QuerySingleOrDefaultAsync<SaasGovernanceEvent>(new CommandDefinition(sql, new { organizationId, global, id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SaasNotification>> ListNotificationsAsync(Guid organizationId, Guid userId, string? type, bool? unread, CancellationToken ct)
    {
        using var connection = factory.Create();
        const string sql = """SELECT id Id,type Type,title Title,message Message,related_module RelatedModule,related_entity_id RelatedEntityId,read_at ReadAt,created_at CreatedAt FROM valorapesquisa.notifications WHERE organization_id=@organizationId AND user_id=@userId AND deleted_at IS NULL AND (@type IS NULL OR type=@type) AND (@unread IS NULL OR (@unread AND read_at IS NULL) OR (NOT @unread AND read_at IS NOT NULL)) ORDER BY created_at DESC LIMIT 100""";
        return (await connection.QueryAsync<SaasNotification>(new CommandDefinition(sql, new { organizationId, userId, type, unread }, cancellationToken: ct))).AsList();
    }

    public async Task<bool> MarkNotificationReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken ct)
    {
        using var connection = factory.Create();
        return await connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.notifications SET read_at=COALESCE(read_at,now()),updated_at=now() WHERE id=@id AND organization_id=@organizationId AND user_id=@userId AND deleted_at IS NULL", new { organizationId, userId, id }, cancellationToken: ct)) == 1;
    }

    public async Task<int> MarkAllNotificationsReadAsync(Guid organizationId, Guid userId, CancellationToken ct)
    {
        using var connection = factory.Create();
        return await connection.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.notifications SET read_at=now(),updated_at=now() WHERE organization_id=@organizationId AND user_id=@userId AND read_at IS NULL AND deleted_at IS NULL", new { organizationId, userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SaasHealthEvent>> ListHealthEventsAsync(CancellationToken ct)
    {
        using var connection = factory.Create();
        const string sql = "SELECT id Id,component Component,status Status,message Message,correlation_id CorrelationId,created_at CreatedAt FROM valorapesquisa.system_health_events WHERE deleted_at IS NULL ORDER BY created_at DESC LIMIT 20";
        return (await connection.QueryAsync<SaasHealthEvent>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<SchemaHealthItem>> GetSchemaHealthAsync(CancellationToken ct)
    {
        using var connection = factory.Create();
        const string sql = """
            SELECT required.component AS Component,
                   CASE WHEN columns.column_name IS NULL THEN 'critical' ELSE 'configured' END AS Status
            FROM (VALUES ('notifications.message','notifications','message'),('api_keys.key_hash','api_keys','key_hash'))
                 AS required(component,table_name,column_name)
            LEFT JOIN information_schema.columns columns
              ON columns.table_schema='valorapesquisa'
             AND columns.table_name=required.table_name
             AND columns.column_name=required.column_name
            ORDER BY required.component
            """;
        return (await connection.QueryAsync<SchemaHealthItem>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }
}
