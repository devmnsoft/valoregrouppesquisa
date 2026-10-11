using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class AuditRepository(
    IDbConnectionFactory connectionFactory,
    ILogger<AuditRepository> logger) : IAuditRepository {
    private const string InsertSql = """
        INSERT INTO valorapesquisa.audit_logs
            (organization_id, user_id, action, entity_type, entity_id, message,
             metadata_json, correlation_id, created_at, ip_hash, user_agent, severity, module)
        VALUES
            (@OrganizationId, @UserId, @Action, @EntityType, @EntityId, @Message,
             CAST(@MetadataJson AS jsonb), @CorrelationId, COALESCE(@CreatedAt, now()), @IpHash,
             @UserAgent, @Severity, @Module)
        """;

    public Task AddAsync(AuditEntry entry) => LogAsync(entry);

    public async Task LogAsync(AuditEntry entry, IDbTransaction? transaction = null) {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateMetadata(entry.MetadataJson);

        try {
            var command = new CommandDefinition(
                InsertSql,
                entry,
                transaction,
                cancellationToken: CancellationToken.None);

            if (transaction?.Connection is not null) {
                await transaction.Connection.ExecuteAsync(command);
                return;
            }

            using var connection = connectionFactory.Create();
            await connection.ExecuteAsync(command);
        }
        catch (Exception exception) {
            logger.LogError(
                exception,
                "Failed to append business audit. Action={Action} EntityType={EntityType} EntityId={EntityId} CorrelationId={CorrelationId}",
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.CorrelationId);
            throw;
        }
    }

    public async Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId, int limit = 100) {
        const string sql = """
            SELECT id,
                   organization_id AS OrganizationId,
                   user_id AS UserId,
                   action,
                   entity_type AS EntityType,
                   entity_id AS EntityId,
                   message,
                   metadata_json::text AS MetadataJson,
                   correlation_id AS CorrelationId,
                   ip_hash AS IpHash,
                   user_agent AS UserAgent,
                   severity,
                   module,
                   created_at AS CreatedAt
              FROM valorapesquisa.audit_logs
             WHERE organization_id = @OrganizationId
             ORDER BY created_at DESC
             LIMIT @Limit
            """;

        var safeLimit = Math.Clamp(limit, 1, 500);
        try {
            using var connection = connectionFactory.Create();
            var command = new CommandDefinition(
                sql,
                new { OrganizationId = organizationId, Limit = safeLimit },
                cancellationToken: CancellationToken.None);
            return (await connection.QueryAsync(command)).AsList();
        }
        catch (Exception exception) {
            logger.LogError(
                exception,
                "Failed to list append-only audit. OrganizationId={OrganizationId}",
                organizationId);
            throw;
        }
    }

    public async Task<(IReadOnlyList<dynamic> Items, long Total)> ListAdminAsync(Guid organizationId, AuditListQuery query, CancellationToken ct = default) {
        const string where = """
            organization_id = @OrganizationId
            AND (@FromUtc::timestamptz IS NULL OR created_at >= @FromUtc)
            AND (@ToUtcExclusive::timestamptz IS NULL OR created_at < @ToUtcExclusive)
            AND (@Action::text IS NULL OR action = @Action)
            AND (@EntityType::text IS NULL OR entity_type = @EntityType)
            AND (@Search::text IS NULL OR action ILIKE '%'||@Search||'%'
                 OR entity_type ILIKE '%'||@Search||'%'
                 OR entity_id ILIKE '%'||@Search||'%'
                 OR message ILIKE '%'||@Search||'%'
                 OR correlation_id ILIKE '%'||@Search||'%')
            """;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var parameters = new {
            OrganizationId = organizationId,
            query.FromUtc,
            query.ToUtcExclusive,
            Action = string.IsNullOrWhiteSpace(query.Action) ? null : query.Action.Trim(),
            EntityType = string.IsNullOrWhiteSpace(query.EntityType) ? null : query.EntityType.Trim(),
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };
        var selectSql = $"""
            SELECT id,
                   organization_id AS OrganizationId,
                   user_id AS UserId,
                   action,
                   entity_type AS EntityType,
                   entity_id AS EntityId,
                   message,
                   metadata_json::text AS MetadataJson,
                   correlation_id AS CorrelationId,
                   ip_hash AS IpHash,
                   user_agent AS UserAgent,
                   severity,
                   module,
                   created_at AS CreatedAt
              FROM valorapesquisa.audit_logs
             WHERE {where}
             ORDER BY created_at DESC, id DESC
             LIMIT @PageSize OFFSET @Offset
            """;
        var countSql = $"SELECT count(*) FROM valorapesquisa.audit_logs WHERE {where}";
        try {
            using var connection = connectionFactory.Create();
            var items = (await connection.QueryAsync(new CommandDefinition(selectSql, parameters, cancellationToken: ct))).AsList();
            var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(countSql, parameters, cancellationToken: ct));
            return (items, total);
        }
        catch (Exception exception) {
            logger.LogError(exception, "Failed to search append-only audit. OrganizationId={OrganizationId}", organizationId);
            throw;
        }
    }

    private static void ValidateMetadata(string metadataJson) {
        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object) {
            throw new ArgumentException("Audit metadata must be a JSON object.", nameof(metadataJson));
        }
    }
}
